param([string]$Server='localhost')
$ErrorActionPreference='Stop'
$target='SWD392_BookSales_3NF_REVIEW_20261027'
$evidence=Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) '.artifacts\database-normalization-20261027'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
function Open-TestConnection {
    $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source']=$Server; $builder['Initial Catalog']=$target
    $builder['Integrated Security']=$true; $builder['Pooling']=$false
    $connection=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $connection.Open(); return $connection
}
function Invoke-TestSql($connection,[string]$sql) {
    $command=$connection.CreateCommand();$command.CommandText=$sql;$command.CommandTimeout=30
    try {return $command.ExecuteNonQuery()} finally {$command.Dispose()}
}
$emptyCheck=@'
IF EXISTS (
 SELECT 1 FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
 WHERE t.is_ms_shipped=0 AND p.rows<>0
) THROW 51990, 'Tests require an empty review database; existing data is never deleted.', 1;
'@
$connection=Open-TestConnection
try {[void](Invoke-TestSql $connection $emptyCheck)} finally {$connection.Dispose()}
$seed=@'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT dbo.Employee(employee_id,name,role_code) VALUES (N'E1',N'Test Inventory',N'INVENTORY_STAFF'),(N'E2',N'Test Sales',N'SALES_STAFF');
INSERT dbo.Account(account_id,employee_id,username,status) VALUES (N'A1',N'E1',N'test_inventory',N'active');
INSERT dbo.Permission(permission_id,permission_name) VALUES (N'P1',N'Test capability');
INSERT dbo.Book(book_id,title,current_price) VALUES (N'B1',N'Test Book 1',100),(N'B2',N'Test Book 2',200);
INSERT dbo.BookStock(book_id,current_quantity) VALUES (N'B1',10),(N'B2',0);
INSERT dbo.InventoryMovement(posting_key,book_id,quantity_delta,occurred_at_utc) VALUES (N'test-opening:B1',N'B1',10,'2026-10-27T00:00:00');
INSERT dbo.SalesOrder(order_id,order_date,total_order_amount) VALUES (N'O1','2026-10-27',1200),(N'O2','2026-10-27',600);
INSERT dbo.SalesOrderLine(order_id,book_id,quantity,unit_price) VALUES (N'O1',N'B1',5,100);
DECLARE @sale1 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.SalesOrderLine(order_id,book_id,quantity,unit_price) VALUES (N'O1',N'B1',7,100);
DECLARE @sale2 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.SalesOrderLine(order_id,book_id,quantity,unit_price) VALUES (N'O2',N'B2',3,200);
DECLARE @otherSale bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.PurchaseOrder(purchase_order_id) VALUES (N'PO1'),(N'PO2');
INSERT dbo.PurchaseOrderLine(purchase_order_id,book_id,ordered_quantity) VALUES (N'PO1',N'B1',10);
DECLARE @po1 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.PurchaseOrderLine(purchase_order_id,book_id,ordered_quantity) VALUES (N'PO1',N'B1',20);
DECLARE @po2 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.PurchaseOrderLine(purchase_order_id,book_id,ordered_quantity) VALUES (N'PO2',N'B2',5);
DECLARE @poOther bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.GoodsReceipt(receipt_id,receipt_date) VALUES (N'GR1','2026-10-27'),(N'GR2','2026-10-27');
INSERT dbo.InventoryAudit(audit_id,audit_date,status) VALUES (N'AU1','2026-10-27',N'approved');
INSERT dbo.InventoryCount(count_id,counted_at_utc,recorded_by_employee_id) VALUES (N'C1','2026-10-27T00:00:00',N'E1'),(N'C2','2026-10-27T01:00:00',N'E1');
INSERT dbo.InventoryCountObservation(count_id,book_id,counted_quantity,observed_at_utc) VALUES (N'C1',N'B1',18,'2026-10-27T00:00:00');
DECLARE @obs1 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.InventoryCountObservation(count_id,book_id,counted_quantity,observed_at_utc) VALUES (N'C1',N'B1',20,'2026-10-27T00:01:00');
DECLARE @obs2 bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.InventoryCountObservation(count_id,book_id,counted_quantity,observed_at_utc) VALUES (N'C2',N'B1',19,'2026-10-27T01:00:00');
DECLARE @otherObs bigint=CONVERT(bigint,SCOPE_IDENTITY());
INSERT dbo.InventoryAdjustment(adjustment_id,reason) VALUES (N'AD1',N'test'),(N'AD2',N'test');
INSERT dbo.BookReturn(return_id,original_order_id,returned_at_utc) VALUES (N'R1',N'O1','2026-10-27T01:00:00'),(N'R2',N'O1','2026-10-27T02:00:00'),(N'R3',N'O2','2026-10-27T03:00:00');
'@
$cases=@(
    @{name='Repeated Book sales lines allowed';sql="IF (SELECT COUNT(*) FROM dbo.SalesOrderLine WHERE order_id=N'O1' AND book_id=N'B1')<>2 THROW 51999,'Repeated sale lost',1;"},
    @{name='Repeated Book PO lines allowed';sql="IF (SELECT COUNT(*) FROM dbo.PurchaseOrderLine WHERE purchase_order_id=N'PO1' AND book_id=N'B1')<>2 THROW 51999,'Repeated PO lost',1;"},
    @{name='Receipt combines multiple POs';sql="INSERT dbo.GoodsReceiptLine(receipt_id,purchase_order_line_id,received_quantity) VALUES(N'GR1',@po1,2),(N'GR1',@poOther,3); IF (SELECT COUNT(DISTINCT purchase_order_id) FROM dbo.vGoodsReceiptSources WHERE receipt_id=N'GR1')<>2 THROW 51999,'Multi-PO not retained',1;"},
    @{name='PO received across multiple receipts';sql="INSERT dbo.GoodsReceiptLine(receipt_id,purchase_order_line_id,received_quantity) VALUES(N'GR1',@po1,2),(N'GR2',@po1,3); IF (SELECT COUNT(DISTINCT receipt_id) FROM dbo.vGoodsReceiptSources WHERE purchase_order_id=N'PO1')<>2 THROW 51999,'Partial receipts lost',1;"},
    @{name='Repeated receipt Book lines retain PO-line identity';sql="INSERT dbo.GoodsReceiptLine(receipt_id,purchase_order_line_id,received_quantity) VALUES(N'GR1',@po1,2),(N'GR1',@po2,3); IF (SELECT COUNT(*) FROM dbo.vGoodsReceiptSources WHERE receipt_id=N'GR1' AND book_id=N'B1')<>2 THROW 51999,'Receipt identity lost',1;"},
    @{name='Recount observations retained';sql="IF (SELECT COUNT(*) FROM dbo.InventoryCountObservation WHERE count_id=N'C1' AND book_id=N'B1')<>2 THROW 51999,'Recount history lost',1;"},
    @{name='Selected recount is 20, not 38';sql="INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B1',@obs2,10); IF (SELECT counted_quantity FROM dbo.vSelectedInventoryCount WHERE count_id=N'C1' AND book_id=N'B1')<>20 THROW 51999,'Recounts were summed',1;"},
    @{name='Earlier observation may be explicitly selected';sql="INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B1',@obs1,10); IF (SELECT counted_quantity FROM dbo.vSelectedInventoryCount WHERE count_id=N'C1' AND book_id=N'B1')<>18 THROW 51999,'Selection incorrectly uses MAX(time)',1;"},
    @{name='Selection rejects observation from another Count';error=547;sql="INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B1',@otherObs,10);"},
    @{name='Selection rejects observation for another Book';error=547;sql="INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B2',@obs2,0);"},
    @{name='One selected result per Count and Book';error=2627;sql="INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B1',@obs1,10),(N'C1',N'B1',@obs2,10);"},
    @{name='One Account per Employee';error=2627;sql="INSERT dbo.Account(account_id,employee_id,username) VALUES(N'A2',N'E1',N'different_username');"},
    @{name='Unique Username under proposed login key';error=2627;sql="INSERT dbo.Account(account_id,employee_id,username) VALUES(N'A2',N'E2',N'test_inventory');"},
    @{name='Primary Role constrained to current role codes';error=547;sql="UPDATE dbo.Employee SET role_code=N'UNKNOWN' WHERE employee_id=N'E1';"},
    @{name='Permission grant requires an existing Permission';error=547;sql="INSERT dbo.EmployeePermission VALUES(N'E1',N'UNKNOWN');"},
    @{name='One resulting Adjustment per Count';error=2627;sql="INSERT dbo.CountAdjustment VALUES(N'C1',N'AD1'),(N'C1',N'AD2');"},
    @{name='Automatic Adjustment has one source Count';error=2627;sql="INSERT dbo.CountAdjustment VALUES(N'C1',N'AD1'),(N'C2',N'AD1');"},
    @{name='One Adjustment covers differing Books';sql="INSERT dbo.CountAdjustment VALUES(N'C1',N'AD1'); INSERT dbo.AdjustmentBook VALUES(N'AD1',N'B1',10),(N'AD1',N'B2',5); IF (SELECT COUNT(*) FROM dbo.AdjustmentBook WHERE adjustment_id=N'AD1')<>2 THROW 51999,'Adjustment did not retain both books',1;"},
    @{name='Audit approval has no implicit stock or Adjustment effect';sql="INSERT dbo.InventoryAuditLine(audit_id,book_id,counted_quantity) VALUES(N'AU1',N'B1',99); UPDATE dbo.InventoryAudit SET status=N'approved' WHERE audit_id=N'AU1'; IF (SELECT current_quantity FROM dbo.BookStock WHERE book_id=N'B1')<>10 OR EXISTS(SELECT 1 FROM dbo.AdjustmentBook) OR EXISTS(SELECT 1 FROM dbo.CountAdjustment) THROW 51999,'Audit changed inventory',1;"},
    @{name='Multiple partial returns against one original line';sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,3,300),(N'R2',@sale1,2,200); IF (SELECT SUM(returned_quantity) FROM dbo.BookReturnLine WHERE original_order_line_id=@sale1)<>5 THROW 51999,'Partial returns lost',1;"},
    @{name='Repeated Book sales lines have distinct return limits';sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,5,500),(N'R1',@sale2,7,700);"},
    @{name='Return cannot reference a line from another order';error=51010;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R3',@sale1,1,100);"},
    @{name='One return cannot exceed original sales quantity';error=51011;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,6,600);"},
    @{name='Cumulative returns cannot exceed original sales quantity';error=51011;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,4,400); INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R2',@sale1,2,200);"},
    @{name='Changing Return header cannot invalidate its lines';error=51010;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,1,100); UPDATE dbo.BookReturn SET original_order_id=N'O2' WHERE return_id=N'R1';"},
    @{name='Reducing original sale quantity cannot invalidate returns';error=51011;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,4,400); UPDATE dbo.SalesOrderLine SET quantity=3 WHERE order_line_id=@sale1;"},
    @{name='Moving original sale line cannot invalidate returns';error=51010;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,1,100); UPDATE dbo.SalesOrderLine SET order_id=N'O2' WHERE order_line_id=@sale1;"},
    @{name='Refund amount cannot be negative';error=547;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,1,-1);"},
    @{name='Return quantity must be positive';error=547;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',@sale1,0,0);"},
    @{name='Receipt must reference an existing PO line';error=547;sql="INSERT dbo.GoodsReceiptLine(receipt_id,purchase_order_line_id,received_quantity) VALUES(N'GR1',-1,2);"},
    @{name='Return must reference an existing sales line';error=547;sql="INSERT dbo.BookReturnLine(return_id,original_order_line_id,returned_quantity,refund_amount) VALUES(N'R1',-1,1,100);"},
    @{name='Posting key rejects duplicate movement';error=2627;sql="INSERT dbo.InventoryMovement(posting_key,book_id,quantity_delta,occurred_at_utc) VALUES(N'test-opening:B1',N'B1',10,'2026-10-27');"},
    @{name='Stored stock reconciles with opening movement';sql="IF EXISTS(SELECT 1 FROM dbo.vStockReconciliation WHERE difference<>0) THROW 51999,'Stock mismatch',1;"},
    @{name='Receipt line does not repeat PO ID or Book ID';sql="IF COL_LENGTH('dbo.GoodsReceiptLine','book_id') IS NOT NULL OR COL_LENGTH('dbo.GoodsReceiptLine','purchase_order_id') IS NOT NULL THROW 51999,'Transitive attributes repeated',1;"},
    @{name='Return line does not repeat original order or Book';sql="IF COL_LENGTH('dbo.BookReturnLine','book_id') IS NOT NULL OR COL_LENGTH('dbo.BookReturnLine','original_order_id') IS NOT NULL THROW 51999,'Transitive attributes repeated',1;"},
    @{name='Adjustment header has no ambiguous quantity';sql="IF COL_LENGTH('dbo.InventoryAdjustment','quantity_change') IS NOT NULL THROW 51999,'Quantity still on header',1;"},
    @{name='Count difference retains all 10 decimal places';sql="UPDATE dbo.InventoryCountObservation SET counted_quantity=10.0000000001 WHERE observation_id=@obs2; INSERT dbo.InventoryCountSelection VALUES(N'C1',N'B1',@obs2,10); IF (SELECT quantity_difference FROM dbo.vSelectedInventoryCount WHERE count_id=N'C1' AND book_id=N'B1')<>CONVERT(decimal(38,10),0.0000000001) THROW 51999,'Difference scale lost',1;"},
    @{name='Stock reconciliation retains all 10 decimal places';sql="UPDATE dbo.BookStock SET current_quantity=10.0000000001 WHERE book_id=N'B1'; IF (SELECT difference FROM dbo.vStockReconciliation WHERE book_id=N'B1')<>CONVERT(decimal(38,10),0.0000000001) THROW 51999,'Ledger difference scale lost',1;"}
)
$results=@()
foreach($case in $cases) {
    $connection=Open-TestConnection
    $actual=$null;$message=$null;$cleanupError=$null
    try {[void](Invoke-TestSql $connection ($seed+"`n"+$case.sql))}
    catch {
        $errorObject=$_.Exception
        while($errorObject.InnerException -and -not ($errorObject -is [System.Data.SqlClient.SqlException])) {$errorObject=$errorObject.InnerException}
        if($errorObject -is [System.Data.SqlClient.SqlException]) {
            $numbers=@($errorObject.Errors | ForEach-Object {$_.Number})
            if($case.ContainsKey('error') -and $numbers -contains $case.error) {$actual=$case.error} else {$actual=$errorObject.Number}
        } else {$actual=-1}
        $message=$errorObject.Message
    } finally {
        try {[void](Invoke-TestSql $connection 'IF XACT_STATE()<>0 ROLLBACK TRANSACTION;')}
        catch {$cleanupError=$_.Exception.Message}
        $connection.Dispose()
    }
    $expected=if($case.ContainsKey('error')) {$case.error} else {$null}
    $passed=($actual -eq $expected) -and ($null -eq $cleanupError)
    $results+=,[pscustomobject]@{name=$case.name;passed=$passed;expected_sql_error=$expected;actual_sql_error=$actual;message=$message;cleanup_error=$cleanupError}
}
$connection=Open-TestConnection
try {[void](Invoke-TestSql $connection $emptyCheck)} finally {$connection.Dispose()}
$result=[ordered]@{database=$target;synthetic_data_rolled_back=$true;passed=@($results | Where-Object {$_.passed}).Count;failed=@($results | Where-Object {-not $_.passed}).Count;tests=$results;scope='Single-session SQL integrity checks only; not concurrent load, UI, stock-posting workflow or refund eligibility/calculation tests.'}
ConvertTo-Json -InputObject $result -Depth 15 | Set-Content (Join-Path $evidence 'semantic-tests.json') -Encoding UTF8
if($result.failed -gt 0) {
    $results | Where-Object {-not $_.passed} | Format-List
    throw "$($result.failed) SQL semantic tests failed. See evidence."
}
Write-Output "PASS: $($result.passed) SQL semantic checks; all synthetic business rows rolled back."
