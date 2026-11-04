param([string]$Server='localhost',[switch]$VerifyBaseline)
$ErrorActionPreference='Stop'
$target='SWD392_BookSales_3NF_REVIEW_20261027'
$repositoryRoot=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$evidence=Join-Path $repositoryRoot '.artifacts\demo-data-20261027'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
function Open-DemoConnection([string]$database) {
    $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source']=$Server;$builder['Initial Catalog']=$database
    $builder['Integrated Security']=$true;$builder['Application Name']='SWD392 explicit fictional demo fixtures'
    $builder['Pooling']=$false;$builder['Connect Timeout']=10
    $connection=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $connection.Open();return $connection
}
function Query-Demo($connection,[string]$sql) {
    $command=$connection.CreateCommand();$command.CommandText=$sql;$command.CommandTimeout=120
    $data=New-Object System.Data.DataSet;$adapter=New-Object System.Data.SqlClient.SqlDataAdapter($command)
    try {
        [void]$adapter.Fill($data);$sets=@()
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
function Get-DemoBatches([string]$file) {
    return @([regex]::Split((Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw -Encoding UTF8),'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$') | Where-Object {$_.Trim()})
}
function Digest-Database([string]$database) {
    $connection=Open-DemoConnection $database
    try {
        $metadata=Query-Demo $connection @'
SELECT t.name AS table_name,SCHEMA_NAME(t.schema_id) AS schema_name FROM sys.tables t WHERE t.is_ms_shipped=0 ORDER BY schema_name,t.name;
SELECT t.name AS table_name,c.name AS column_name,ic.key_ordinal FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,ic.key_ordinal;
SELECT object_id,name,type,CONVERT(varchar(33),modify_date,126) AS modified FROM sys.objects WHERE is_ms_shipped=0 ORDER BY object_id;
SELECT object_id,column_id,name,user_type_id,max_length,precision,scale,is_nullable FROM sys.columns WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped=0) ORDER BY object_id,column_id;
'@
        $digests=@();$sha=[System.Security.Cryptography.SHA256]::Create()
        try {
            foreach($table in $metadata[0].rows) {
                $schema='['+$table.schema_name.Replace(']',']]')+']'
                $name='['+$table.table_name.Replace(']',']]')+']'
                $keys=@($metadata[1].rows | Where-Object {$_.table_name -eq $table.table_name} | Sort-Object key_ordinal | ForEach-Object {'['+$_.column_name.Replace(']',']]')+']'})
                $order=if($keys.Count) {' ORDER BY '+($keys -join ',')} else {''}
                $command=$connection.CreateCommand()
                $command.CommandText="SELECT (SELECT * FROM $schema.$name$order FOR JSON PATH,INCLUDE_NULL_VALUES) AS payload;"
                try {$payload=[string]$command.ExecuteScalar()} finally {$command.Dispose()}
                $hash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($payload))).Replace('-','').ToLowerInvariant()
                $digests+=,[pscustomobject]@{table=$table.table_name;sha256=$hash}
            }
        } finally {$sha.Dispose()}
        return [pscustomobject]@{database=$database;tables=$digests;object_metadata=$metadata[2].rows;column_metadata=$metadata[3].rows}
    } finally {$connection.Dispose()}
}
$legacyBefore=Digest-Database 'SWD392_BookSales_DEV_DRAFT'
$reviewBefore=Digest-Database $target
$connection=Open-DemoConnection $target
try {
    if($VerifyBaseline) {
        $results=@()
        foreach($batch in (Get-DemoBatches 'verify_demo.sql')) {$results+=Query-Demo $connection $batch}
        $mode='VerifyBaseline'
    } else {
        # Parse in a separate session; this never executes the seed.
        $parse=Open-DemoConnection $target
        try {
            [void](Query-Demo $parse 'SET PARSEONLY ON;')
            foreach($batch in (Get-DemoBatches 'seed_demo.sql')) {[void](Query-Demo $parse $batch)}
        } finally {$parse.Dispose()}
        $results=@()
        foreach($batch in (Get-DemoBatches 'seed_demo.sql')) {$results+=Query-Demo $connection $batch}
        $mode=[string]$results[0].rows[0].seed_status
        if($mode -eq 'SEEDED') {
            foreach($batch in (Get-DemoBatches 'verify_demo.sql')) {$results+=Query-Demo $connection $batch}
        }
    }
} finally {$connection.Dispose()}
$legacyAfter=Digest-Database 'SWD392_BookSales_DEV_DRAFT'
$reviewAfter=Digest-Database $target
$legacyUnchanged=(ConvertTo-Json -InputObject $legacyBefore -Depth 20 -Compress) -ceq (ConvertTo-Json -InputObject $legacyAfter -Depth 20 -Compress)
if(-not $legacyUnchanged) {throw 'Legacy database changed; inspect before/after state. Seed does not write that database.'}
$schemaUnchanged=(ConvertTo-Json -InputObject @($reviewBefore.object_metadata,$reviewBefore.column_metadata) -Depth 20 -Compress) -ceq (ConvertTo-Json -InputObject @($reviewAfter.object_metadata,$reviewAfter.column_metadata) -Depth 20 -Compress)
if(-not $schemaUnchanged) {throw 'Review schema changed during seed; inspect state.'}
$idempotent=if($mode -eq 'ALREADY_SEEDED') {(ConvertTo-Json -InputObject $reviewBefore -Depth 20 -Compress) -ceq (ConvertTo-Json -InputObject $reviewAfter -Depth 20 -Compress)} else {$null}
if($mode -eq 'ALREADY_SEEDED' -and -not $idempotent) {throw 'Repeated seed changed table contents.'}
$result=[ordered]@{status='PASS';mode=$mode;database=$target;legacy_unchanged=$legacyUnchanged;schema_unchanged=$schemaUnchanged;repeat_content_unchanged=$idempotent;results=$results;before=$reviewBefore;after=$reviewAfter}
$file=if($mode -eq 'ALREADY_SEEDED') {'rerun-result.json'} elseif($VerifyBaseline) {'baseline-result.json'} else {'seed-result.json'}
ConvertTo-Json -InputObject $result -Depth 25 | Set-Content -LiteralPath (Join-Path $evidence $file) -Encoding UTF8
Write-Output "PASS: $mode; legacy/schema unchanged. Evidence: $evidence"
