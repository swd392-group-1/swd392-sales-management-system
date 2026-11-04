param([string]$Server='localhost', [switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$target='SWD392_BookSales_3NF_REVIEW_20261027'
$legacy='SWD392_BookSales_DEV_DRAFT'
$evidence=Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.artifacts\database-normalization-20261027'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
function Open-ReviewConnection([string]$database) {
    $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source']=$Server
    $builder['Initial Catalog']=$database
    $builder['Integrated Security']=$true
    $builder['Application Name']='SWD392 separate normalization review'
    $builder['Connect Timeout']=10
    $builder['Pooling']=$false
    $connection=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $connection.Open()
    return $connection
}
function Query-Review($connection,[string]$sql) {
    $command=$connection.CreateCommand(); $command.CommandText=$sql; $command.CommandTimeout=120
    $data=New-Object System.Data.DataSet
    $adapter=New-Object System.Data.SqlClient.SqlDataAdapter($command)
    try {
        [void]$adapter.Fill($data)
        $sets=@()
        foreach($table in $data.Tables) {
            $rows=@()
            foreach($row in $table.Rows) {
                $record=[ordered]@{}
                foreach($column in $table.Columns) {
                    $value=$row[$column.ColumnName]
                    if($value -is [DBNull]) {$value=$null}
                    if($value -is [DateTime]) {$value=$value.ToString('o')}
                    $record[$column.ColumnName]=$value
                }
                $rows+=,[pscustomobject]$record
            }
            $sets+=,[pscustomobject]@{rows=$rows}
        }
        return ,$sets
    } finally {$adapter.Dispose();$data.Dispose();$command.Dispose()}
}
function Get-LegacySnapshot {
    $connection=Open-ReviewConnection $legacy
    try {
        return ,(Query-Review $connection @'
SELECT o.object_id,o.name,o.type,CONVERT(varchar(33),o.modify_date,126) AS modified FROM sys.objects o WHERE o.is_ms_shipped=0 ORDER BY o.object_id;
SELECT object_id,column_id,name,user_type_id,max_length,precision,scale,is_nullable FROM sys.columns WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped=0) ORDER BY object_id,column_id;
SELECT name,parent_object_id,referenced_object_id,is_disabled,is_not_trusted FROM sys.foreign_keys ORDER BY name;
SELECT t.name,SUM(p.rows) AS row_count FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1) WHERE t.is_ms_shipped=0 GROUP BY t.name ORDER BY t.name;
'@)
    } finally {$connection.Dispose()}
}
function Get-ReviewBatches([string]$file) {
    return @([regex]::Split((Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw -Encoding UTF8),'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$') | Where-Object {$_.Trim()})
}
$journal=@(); $before=Get-LegacySnapshot
$prefix=if($VerifyOnly) {'verification-'} else {''}
ConvertTo-Json -InputObject $before -Depth 20 | Set-Content (Join-Path $evidence ($prefix+'legacy-before.json')) -Encoding UTF8
$master=Open-ReviewConnection 'master'
try {
    $preflight=Query-Review $master "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) AS server_name,CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) AS version,DB_ID(N'$target') AS target_id;"
    if(-not $VerifyOnly -and $null -ne $preflight[0].rows[0].target_id) {throw 'Review target already exists; refusing to overwrite.'}
    if($VerifyOnly -and $null -eq $preflight[0].rows[0].target_id) {throw 'Review target absent.'}
    if(-not $VerifyOnly) {
        foreach($file in @('01_create_database.sql','02_create_schema.sql','03_views_and_return_rules.sql','04_verify_schema.sql')) {
            $parse=Open-ReviewConnection 'master'
            try {
                [void](Query-Review $parse 'SET PARSEONLY ON;')
                foreach($batch in (Get-ReviewBatches $file)) {
                    $syntax=[regex]::Replace($batch,'(?im)^\s*USE\s+\[[^\]]+\]\s*;\s*$','')
                    if($syntax.Trim()) {[void](Query-Review $parse $syntax)}
                }
                $journal+=,[pscustomobject]@{phase='PARSEONLY';file=$file;result='PASS (USE omitted; no DDL executed)'}
            } finally {$parse.Dispose()}
        }
        foreach($file in @('01_create_database.sql','02_create_schema.sql','03_views_and_return_rules.sql')) {
            $connection=Open-ReviewConnection 'master'
            try {
                foreach($batch in (Get-ReviewBatches $file)) {[void](Query-Review $connection $batch)}
                $journal+=,[pscustomobject]@{phase='EXECUTE';file=$file;result='PASS'}
            } finally {$connection.Dispose()}
        }
    }
    $connection=Open-ReviewConnection $target
    try {
        $catalog=@()
        foreach($batch in (Get-ReviewBatches '04_verify_schema.sql')) {$catalog+=Query-Review $connection $batch}
        $catalog+=Query-Review $connection @'
SELECT t.name,c.column_id,c.name AS column_name,TYPE_NAME(c.user_type_id) AS sql_type,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id;
SELECT t.name,SUM(p.rows) AS row_count FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1) WHERE t.is_ms_shipped=0 GROUP BY t.name ORDER BY t.name;
SELECT name,is_disabled FROM sys.triggers WHERE parent_class=1 ORDER BY name;
SELECT name FROM sys.views ORDER BY name;
SELECT name,CONVERT(varchar(33),create_date,126) AS created,compatibility_level,collation_name FROM sys.databases WHERE name=DB_NAME();
SELECT t.name AS table_name,k.name AS constraint_name,k.type,ic.key_ordinal,c.name AS column_name FROM sys.key_constraints k JOIN sys.tables t ON t.object_id=k.parent_object_id JOIN sys.index_columns ic ON ic.object_id=k.parent_object_id AND ic.index_id=k.unique_index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id ORDER BY t.name,k.name,ic.key_ordinal;
SELECT t.name AS table_name,f.name AS constraint_name,fc.constraint_column_id,c.name AS column_name,rt.name AS referenced_table,rc.name AS referenced_column FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.tables rt ON rt.object_id=f.referenced_object_id JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=fc.parent_column_id JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=fc.referenced_column_id ORDER BY t.name,f.name,fc.constraint_column_id;
'@
        ConvertTo-Json -InputObject $catalog -Depth 20 | Set-Content (Join-Path $evidence 'catalog.json') -Encoding UTF8
    } finally {$connection.Dispose()}
    $after=Get-LegacySnapshot
    ConvertTo-Json -InputObject $after -Depth 20 | Set-Content (Join-Path $evidence ($prefix+'legacy-after.json')) -Encoding UTF8
    $unchanged=(ConvertTo-Json -InputObject $before -Depth 20 -Compress) -ceq (ConvertTo-Json -InputObject $after -Depth 20 -Compress)
    if(-not $unchanged) {throw 'Legacy snapshot changed; inspect evidence. No automatic cleanup/drop is performed.'}
    $hashes=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.sql' | ForEach-Object { $hash=Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256; [pscustomobject]@{file=$_.Name;sha256=$hash.Hash} })
    $result=[ordered]@{status='PASS';mode=if($VerifyOnly){'VerifyOnly'}else{'CreateAndVerify'};database=$target;preflight=$preflight;legacy_unchanged=$unchanged;schema_review_status='PROPOSAL — not yet approved';journal=$journal;sql_hashes=$hashes}
    $resultFile=if($VerifyOnly) {'verification-result.json'} else {'execution-result.json'}
    ConvertTo-Json -InputObject $result -Depth 20 | Set-Content (Join-Path $evidence $resultFile) -Encoding UTF8
    Write-Output "PASS: separate review database verified; legacy unchanged. Evidence: $evidence"
} finally {$master.Dispose()}
