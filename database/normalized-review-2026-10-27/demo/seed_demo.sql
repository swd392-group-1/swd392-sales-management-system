-- Book Sales Management System — FICTIONAL DEMO DATA ONLY.
-- Dataset v1; no business-policy approval is implied.
USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

DECLARE @lock_result int; EXEC @lock_result=sys.sp_getapplock @Resource=N'SWD392:demo:20261027:v1',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
IF @lock_result<0 THROW 51200,'Could not acquire demo seed lock.',1;

IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'SWD392_Demo_20261027_v1')
BEGIN
 IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'SWD392_Demo_20261027_v1' AND CONVERT(nvarchar(4000),value)=N'caa111a435f7f2121d758df77eaa8159a10e85f5122b7cc2bc33469d2716b96f') THROW 51201,'Seed marker differs. No existing demo data is overwritten.',1;
 COMMIT TRANSACTION;
 SELECT N'ALREADY_SEEDED' AS seed_status;
 RETURN;
END;

DECLARE @FixtureIds TABLE(entity nvarchar(50) NOT NULL,fixture_key nvarchar(100) NOT NULL,identity_value bigint NOT NULL,PRIMARY KEY(entity,fixture_key));

IF EXISTS(SELECT 1 FROM dbo.[Employee] WHERE [employee_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Account] WHERE [account_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Permission] WHERE [permission_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[EmployeePermission] WHERE [employee_id] LIKE N'DEMO-20261027-%' OR [permission_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Customer] WHERE [customer_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Provider] WHERE [provider_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Book] WHERE [book_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Promotion] WHERE [promotion_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[PromotionBook] WHERE [promotion_id] LIKE N'DEMO-20261027-%' OR [book_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[SalesOrder] WHERE [order_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[Payment] WHERE [payment_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[OrderPayment] WHERE [order_id] LIKE N'DEMO-20261027-%' OR [payment_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[BookReturn] WHERE [return_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[PurchaseOrder] WHERE [purchase_order_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[PurchaseOrderProvider] WHERE [purchase_order_id] LIKE N'DEMO-20261027-%' OR [provider_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[GoodsReceipt] WHERE [receipt_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[ProviderShipment] WHERE [provider_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[PurchaseOrderShipment] WHERE [purchase_order_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[PurchaseOrderApproval] WHERE [employee_id] LIKE N'DEMO-20261027-%' OR [purchase_order_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[InventoryAudit] WHERE [audit_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[InventoryCount] WHERE [count_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[InventoryCountSelection] WHERE [count_id] LIKE N'DEMO-20261027-%' OR [book_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[InventoryAdjustment] WHERE [adjustment_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[AdjustmentBook] WHERE [adjustment_id] LIKE N'DEMO-20261027-%' OR [book_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[CountAdjustment] WHERE [count_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.[BookStock] WHERE [book_id] LIKE N'DEMO-20261027-%') THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.Account WHERE username IN (N'swd392_demo_admin',N'swd392_demo_manager',N'swd392_demo_inventory',N'swd392_demo_sales_a',N'swd392_demo_sales_b',N'swd392_demo_disabled')) THROW 51203,'Demo username already exists; refusing to overwrite.',1;

IF EXISTS(SELECT 1 FROM dbo.InventoryMovement WHERE posting_key LIKE N'DEMO-20261027-%') OR EXISTS(SELECT 1 FROM dbo.Shipment WHERE tracking_number LIKE N'DEMO-20261027-%') OR EXISTS(SELECT 1 FROM dbo.UserAccessLog WHERE request_path LIKE N'/__demo/swd392/20261027%') OR EXISTS(SELECT 1 FROM dbo.ExternalServiceLog WHERE service_name LIKE N'DEMO/SWD392/%') THROW 51202,'Demo namespace already contains unmarked records.',1;

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'Quản trị viên mẫu [DEMO]',N'SYSTEM_ADMIN');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-ADMIN',N'DEMO-20261027-EMP-ADMIN',N'swd392_demo_admin',N'ACTIVE');

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'Quản lý mẫu [DEMO]',N'STORE_MANAGER');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-MANAGER',N'DEMO-20261027-EMP-MANAGER',N'swd392_demo_manager',N'ACTIVE');

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'Nhân viên kho mẫu [DEMO]',N'INVENTORY_STAFF');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-INVENTORY',N'DEMO-20261027-EMP-INVENTORY',N'swd392_demo_inventory',N'ACTIVE');

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-SALES-A',N'Nhân viên bán hàng mẫu A [DEMO]',N'SALES_STAFF');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-SALES-A',N'DEMO-20261027-EMP-SALES-A',N'swd392_demo_sales_a',N'ACTIVE');

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-SALES-B',N'Nhân viên bán hàng mẫu B [DEMO]',N'SALES_STAFF');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-SALES-B',N'DEMO-20261027-EMP-SALES-B',N'swd392_demo_sales_b',N'ACTIVE');

INSERT dbo.[Employee] ([employee_id],[name],[role_code])
VALUES (N'DEMO-20261027-EMP-DISABLED',N'Tài khoản mẫu đã khóa [DEMO]',N'SALES_STAFF');

INSERT dbo.[Account] ([account_id],[employee_id],[username],[status])
VALUES (N'DEMO-20261027-ACC-DISABLED',N'DEMO-20261027-EMP-DISABLED',N'swd392_demo_disabled',N'DISABLED');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC01',N'Process Sale [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC02',N'Process QR Payment [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC03',N'Maintain Book Information [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC04',N'Maintain Provider Information [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC05',N'Prepare Purchase Order [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC06',N'Record Goods Receipt [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC07',N'Record Inventory Count [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC08',N'Record Inventory Adjustment [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC09',N'Perform Inventory Audit [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC10',N'Update Price [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC11',N'Update Discount/Promotion [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC12',N'Update Staff Permissions [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC13',N'Approve Purchase Order [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC14',N'Approve Inventory Audit [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC15',N'View Management Reports [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC16',N'Manage Accounts [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC17',N'Configure System [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC18',N'Review System Activity Logs [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC19',N'Review Security Audit Logs [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC20',N'Monitor System Alerts [DEMO]');

INSERT dbo.[Permission] ([permission_id],[permission_name])
VALUES (N'DEMO-20261027-UC21',N'Process Book Return [DEMO]');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC01');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC02');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC03');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC04');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC05');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC06');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC07');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC08');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC09');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC10');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC11');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC12');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC13');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC14');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC15');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC16');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC17');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC18');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC19');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC20');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-ADMIN',N'DEMO-20261027-UC21');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC10');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC11');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC12');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC13');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC14');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-UC15');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC03');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC04');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC05');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC06');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC07');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC08');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-INVENTORY',N'DEMO-20261027-UC09');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-SALES-A',N'DEMO-20261027-UC01');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-SALES-A',N'DEMO-20261027-UC02');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-SALES-A',N'DEMO-20261027-UC21');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-SALES-B',N'DEMO-20261027-UC01');

INSERT dbo.[EmployeePermission] ([employee_id],[permission_id])
VALUES (N'DEMO-20261027-EMP-DISABLED',N'DEMO-20261027-UC01');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B01',N'Thiết kế hướng đối tượng [DEMO]',130000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B01',N'DEMO-20261027-B01',30,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B02',N'Lập trình C# căn bản [DEMO]',150000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B02',N'DEMO-20261027-B02',20,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B03',N'Cơ sở dữ liệu thực hành [DEMO]',180000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B03',N'DEMO-20261027-B03',15,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B04',N'Ứng dụng web ASP.NET Core [DEMO]',200000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B04',N'DEMO-20261027-B04',10,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B05',N'Mẫu thiết kế phần mềm [DEMO]',220000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B05',N'DEMO-20261027-B05',8,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B06',N'Kiểm thử phần mềm [DEMO]',120000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B06',N'DEMO-20261027-B06',12,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B07',N'Git cho nhóm phát triển [DEMO]',95000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B07',N'DEMO-20261027-B07',25,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B08',N'Kiến trúc phần mềm [DEMO]',250000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B08',N'DEMO-20261027-B08',6,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B09',N'Cấu trúc dữ liệu [DEMO]',175000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B09',N'DEMO-20261027-B09',18,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B10',N'Mạng máy tính [DEMO]',145000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B10',N'DEMO-20261027-B10',14,N'2026-10-19T00:00:00');

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B11',N'Sách mẫu hết hàng [DEMO]',110000);

INSERT dbo.[Book] ([book_id],[title],[current_price])
VALUES (N'DEMO-20261027-B12',N'Sách mẫu sắp hết [DEMO]',165000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-OPEN-B12',N'DEMO-20261027-B12',1,N'2026-10-19T00:00:00');

INSERT dbo.[Provider] ([provider_id],[name])
VALUES (N'DEMO-20261027-PROVIDER-A',N'Nhà cung cấp mẫu A [DEMO]');

INSERT dbo.[ProviderContact] ([provider_id],[contact_kind],[contact_value]) OUTPUT N'provider_contact',N'A',inserted.[contact_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PROVIDER-A',N'EMAIL',N'provider-a@example.test');

INSERT dbo.[Provider] ([provider_id],[name])
VALUES (N'DEMO-20261027-PROVIDER-B',N'Nhà cung cấp mẫu B [DEMO]');

INSERT dbo.[ProviderContact] ([provider_id],[contact_kind],[contact_value]) OUTPUT N'provider_contact',N'B',inserted.[contact_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PROVIDER-B',N'EMAIL',N'provider-b@example.test');

INSERT dbo.[Customer] ([customer_id],[name])
VALUES (N'DEMO-20261027-CUSTOMER-1',N'Khách hàng mẫu 1 [DEMO]');

INSERT dbo.[CustomerContact] ([customer_id],[contact_kind],[contact_value]) OUTPUT N'customer_contact',N'1',inserted.[contact_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-CUSTOMER-1',N'EMAIL',N'customer-1@example.test');

INSERT dbo.[Customer] ([customer_id],[name])
VALUES (N'DEMO-20261027-CUSTOMER-2',N'Khách hàng mẫu 2 [DEMO]');

INSERT dbo.[CustomerContact] ([customer_id],[contact_kind],[contact_value]) OUTPUT N'customer_contact',N'2',inserted.[contact_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-CUSTOMER-2',N'EMAIL',N'customer-2@example.test');

INSERT dbo.[Customer] ([customer_id],[name])
VALUES (N'DEMO-20261027-CUSTOMER-3',N'Khách hàng mẫu 3 [DEMO]');

INSERT dbo.[CustomerContact] ([customer_id],[contact_kind],[contact_value]) OUTPUT N'customer_contact',N'3',inserted.[contact_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-CUSTOMER-3',N'EMAIL',N'customer-3@example.test');

INSERT dbo.[PurchaseOrder] ([purchase_order_id],[order_date],[status])
VALUES (N'DEMO-20261027-PO-1',N'2026-10-19',N'RECEIVED');

INSERT dbo.[PurchaseOrderProvider] ([purchase_order_id],[provider_id])
VALUES (N'DEMO-20261027-PO-1',N'DEMO-20261027-PROVIDER-A');

INSERT dbo.[PurchaseOrderApproval] ([employee_id],[purchase_order_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-PO-1');

INSERT dbo.[PurchaseOrder] ([purchase_order_id],[order_date],[status])
VALUES (N'DEMO-20261027-PO-2',N'2026-10-19',N'RECEIVED');

INSERT dbo.[PurchaseOrderProvider] ([purchase_order_id],[provider_id])
VALUES (N'DEMO-20261027-PO-2',N'DEMO-20261027-PROVIDER-A');

INSERT dbo.[PurchaseOrderApproval] ([employee_id],[purchase_order_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-PO-2');

INSERT dbo.[PurchaseOrder] ([purchase_order_id],[order_date],[status])
VALUES (N'DEMO-20261027-PO-3',N'2026-10-19',N'APPROVED');

INSERT dbo.[PurchaseOrderProvider] ([purchase_order_id],[provider_id])
VALUES (N'DEMO-20261027-PO-3',N'DEMO-20261027-PROVIDER-B');

INSERT dbo.[PurchaseOrderApproval] ([employee_id],[purchase_order_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-PO-3');

INSERT dbo.[PurchaseOrder] ([purchase_order_id],[order_date],[status])
VALUES (N'DEMO-20261027-PO-4',N'2026-10-19',N'APPROVED');

INSERT dbo.[PurchaseOrderProvider] ([purchase_order_id],[provider_id])
VALUES (N'DEMO-20261027-PO-4',N'DEMO-20261027-PROVIDER-A');

INSERT dbo.[PurchaseOrderApproval] ([employee_id],[purchase_order_id])
VALUES (N'DEMO-20261027-EMP-MANAGER',N'DEMO-20261027-PO-4');

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO1-L1',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-1',N'DEMO-20261027-B01',10);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO1-L2',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-1',N'DEMO-20261027-B01',5);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO1-L3',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-1',N'DEMO-20261027-B02',8);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO2-L1',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-2',N'DEMO-20261027-B03',6);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO2-L2',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-2',N'DEMO-20261027-B04',4);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO3-L1',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-3',N'DEMO-20261027-B05',5);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO3-L2',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-3',N'DEMO-20261027-B11',10);

INSERT dbo.[PurchaseOrderLine] ([purchase_order_id],[book_id],[ordered_quantity]) OUTPUT N'po_line',N'PO4-L1',inserted.[purchase_order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-PO-4',N'DEMO-20261027-B12',5);

INSERT dbo.[GoodsReceipt] ([receipt_id],[receipt_date])
VALUES (N'DEMO-20261027-GR-1',N'2026-10-20');

INSERT dbo.[GoodsReceipt] ([receipt_id],[receipt_date])
VALUES (N'DEMO-20261027-GR-2',N'2026-10-21');

INSERT dbo.[GoodsReceipt] ([receipt_id],[receipt_date])
VALUES (N'DEMO-20261027-GR-3',N'2026-10-22');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR1-L1',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-1',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO1-L1'),6);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR1-L1',N'DEMO-20261027-B01',6,N'2026-10-20T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR1-L2',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-1',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO2-L1'),4);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR1-L2',N'DEMO-20261027-B03',4,N'2026-10-20T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR2-L1',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO1-L1'),4);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR2-L1',N'DEMO-20261027-B01',4,N'2026-10-21T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR2-L2',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO1-L2'),5);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR2-L2',N'DEMO-20261027-B01',5,N'2026-10-21T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR2-L3',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO1-L3'),8);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR2-L3',N'DEMO-20261027-B02',8,N'2026-10-21T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR2-L4',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO2-L1'),2);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR2-L4',N'DEMO-20261027-B03',2,N'2026-10-21T00:30:00');

INSERT dbo.[GoodsReceiptLine] ([receipt_id],[purchase_order_line_id],[received_quantity]) OUTPUT N'receipt_line',N'GR3-L1',inserted.[receipt_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-GR-3',(SELECT identity_value FROM @FixtureIds WHERE entity=N'po_line' AND fixture_key=N'PO2-L2'),4);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RECEIPT-GR3-L1',N'DEMO-20261027-B04',4,N'2026-10-22T00:30:00');

INSERT dbo.[Shipment] ([tracking_number],[shipment_status]) OUTPUT N'shipment',N'1',inserted.[shipment_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-TRACK-1',N'DELIVERED');

INSERT dbo.[ProviderShipment] ([provider_id],[shipment_id])
VALUES (N'DEMO-20261027-PROVIDER-A',(SELECT identity_value FROM @FixtureIds WHERE entity=N'shipment' AND fixture_key=N'1'));

INSERT dbo.[Shipment] ([tracking_number],[shipment_status]) OUTPUT N'shipment',N'2',inserted.[shipment_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-TRACK-2',N'PENDING');

INSERT dbo.[ProviderShipment] ([provider_id],[shipment_id])
VALUES (N'DEMO-20261027-PROVIDER-B',(SELECT identity_value FROM @FixtureIds WHERE entity=N'shipment' AND fixture_key=N'2'));

INSERT dbo.[PurchaseOrderShipment] ([purchase_order_id],[shipment_id])
VALUES (N'DEMO-20261027-PO-1',(SELECT identity_value FROM @FixtureIds WHERE entity=N'shipment' AND fixture_key=N'1'));

INSERT dbo.[PurchaseOrderShipment] ([purchase_order_id],[shipment_id])
VALUES (N'DEMO-20261027-PO-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'shipment' AND fixture_key=N'1'));

INSERT dbo.[PurchaseOrderShipment] ([purchase_order_id],[shipment_id])
VALUES (N'DEMO-20261027-PO-3',(SELECT identity_value FROM @FixtureIds WHERE entity=N'shipment' AND fixture_key=N'2'));

INSERT dbo.[SalesOrder] ([order_id],[order_date],[total_order_amount],[customer_id])
VALUES (N'DEMO-20261027-SALE-1',N'2026-10-22',540000,N'DEMO-20261027-CUSTOMER-1');

INSERT dbo.[Payment] ([payment_id],[payment_method],[payment_status])
VALUES (N'DEMO-20261027-PAYMENT-1',N'CASH',N'COMPLETED');

INSERT dbo.[OrderPayment] ([order_id],[payment_id])
VALUES (N'DEMO-20261027-SALE-1',N'DEMO-20261027-PAYMENT-1');

INSERT dbo.[SalesOrder] ([order_id],[order_date],[total_order_amount],[customer_id])
VALUES (N'DEMO-20261027-SALE-2',N'2026-10-23',560000,N'DEMO-20261027-CUSTOMER-2');

INSERT dbo.[Payment] ([payment_id],[payment_method],[payment_status])
VALUES (N'DEMO-20261027-PAYMENT-2',N'CASH',N'COMPLETED');

INSERT dbo.[OrderPayment] ([order_id],[payment_id])
VALUES (N'DEMO-20261027-SALE-2',N'DEMO-20261027-PAYMENT-2');

INSERT dbo.[SalesOrder] ([order_id],[order_date],[total_order_amount],[customer_id])
VALUES (N'DEMO-20261027-SALE-3',N'2026-10-24',345000,NULL);

INSERT dbo.[Payment] ([payment_id],[payment_method],[payment_status])
VALUES (N'DEMO-20261027-PAYMENT-3',N'CASH',N'COMPLETED');

INSERT dbo.[OrderPayment] ([order_id],[payment_id])
VALUES (N'DEMO-20261027-SALE-3',N'DEMO-20261027-PAYMENT-3');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE1-L1',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-1',N'DEMO-20261027-B01',2,130000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE1-L1',N'DEMO-20261027-B01',-2,N'2026-10-22T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE1-L2',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-1',N'DEMO-20261027-B01',1,130000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE1-L2',N'DEMO-20261027-B01',-1,N'2026-10-22T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE1-L3',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-1',N'DEMO-20261027-B02',1,150000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE1-L3',N'DEMO-20261027-B02',-1,N'2026-10-22T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE2-L1',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-2',N'DEMO-20261027-B03',2,180000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE2-L1',N'DEMO-20261027-B03',-2,N'2026-10-23T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE2-L2',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-2',N'DEMO-20261027-B04',1,200000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE2-L2',N'DEMO-20261027-B04',-1,N'2026-10-23T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE3-L1',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-3',N'DEMO-20261027-B07',1,95000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE3-L1',N'DEMO-20261027-B07',-1,N'2026-10-24T02:00:00');

INSERT dbo.[SalesOrderLine] ([order_id],[book_id],[quantity],[unit_price]) OUTPUT N'sale_line',N'SALE3-L2',inserted.[order_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-SALE-3',N'DEMO-20261027-B08',1,250000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-SALE-SALE3-L2',N'DEMO-20261027-B08',-1,N'2026-10-24T02:00:00');

INSERT dbo.[BookReturn] ([return_id],[original_order_id],[returned_at_utc])
VALUES (N'DEMO-20261027-RETURN-1',N'DEMO-20261027-SALE-1',N'2026-10-25T03:00:00');

INSERT dbo.[BookReturn] ([return_id],[original_order_id],[returned_at_utc])
VALUES (N'DEMO-20261027-RETURN-2',N'DEMO-20261027-SALE-1',N'2026-10-26T03:00:00');

INSERT dbo.[BookReturnLine] ([return_id],[original_order_line_id],[returned_quantity],[refund_amount]) OUTPUT N'return_line',N'RETURN1-L1',inserted.[return_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-RETURN-1',(SELECT identity_value FROM @FixtureIds WHERE entity=N'sale_line' AND fixture_key=N'SALE1-L1'),1,130000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RETURN-RETURN1-L1',N'DEMO-20261027-B01',1,N'2026-10-25T03:00:00');

INSERT dbo.[BookReturnLine] ([return_id],[original_order_line_id],[returned_quantity],[refund_amount]) OUTPUT N'return_line',N'RETURN2-L1',inserted.[return_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-RETURN-2',(SELECT identity_value FROM @FixtureIds WHERE entity=N'sale_line' AND fixture_key=N'SALE1-L1'),1,130000);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-RETURN-RETURN2-L1',N'DEMO-20261027-B01',1,N'2026-10-26T03:00:00');

INSERT dbo.[InventoryCount] ([count_id],[counted_at_utc],[recorded_by_employee_id])
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'2026-10-27T04:00:00',N'DEMO-20261027-EMP-INVENTORY');

INSERT dbo.[InventoryCount] ([count_id],[counted_at_utc],[recorded_by_employee_id])
VALUES (N'DEMO-20261027-COUNT-READY',N'2026-10-27T04:00:00',N'DEMO-20261027-EMP-INVENTORY');

INSERT dbo.[InventoryCount] ([count_id],[counted_at_utc],[recorded_by_employee_id])
VALUES (N'DEMO-20261027-COUNT-NOCHANGE',N'2026-10-27T04:00:00',N'DEMO-20261027-EMP-INVENTORY');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B01-1',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B01',42,N'2026-10-27T04:00:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B01-2',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B01',43,N'2026-10-27T04:01:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B02-1',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B02',28,N'2026-10-27T04:02:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B02-2',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B02',27,N'2026-10-27T04:03:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B03-1',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B03',20,N'2026-10-27T04:04:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'H-B03-2',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B03',18,N'2026-10-27T04:05:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'R-B04-1',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-READY',N'DEMO-20261027-B04',11,N'2026-10-27T04:06:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'R-B04-2',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-READY',N'DEMO-20261027-B04',12,N'2026-10-27T04:07:00');

INSERT dbo.[InventoryCountObservation] ([count_id],[book_id],[counted_quantity],[observed_at_utc]) OUTPUT N'observation',N'N-B07-1',inserted.[observation_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-COUNT-NOCHANGE',N'DEMO-20261027-B07',24,N'2026-10-27T04:08:00');

INSERT dbo.[InventoryCountSelection] ([count_id],[book_id],[selected_observation_id],[recorded_quantity])
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B01',(SELECT identity_value FROM @FixtureIds WHERE entity=N'observation' AND fixture_key=N'H-B01-2'),44);

INSERT dbo.[InventoryCountSelection] ([count_id],[book_id],[selected_observation_id],[recorded_quantity])
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B02',(SELECT identity_value FROM @FixtureIds WHERE entity=N'observation' AND fixture_key=N'H-B02-2'),27);

INSERT dbo.[InventoryCountSelection] ([count_id],[book_id],[selected_observation_id],[recorded_quantity])
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-B03',(SELECT identity_value FROM @FixtureIds WHERE entity=N'observation' AND fixture_key=N'H-B03-2'),19);

INSERT dbo.[InventoryCountSelection] ([count_id],[book_id],[selected_observation_id],[recorded_quantity])
VALUES (N'DEMO-20261027-COUNT-NOCHANGE',N'DEMO-20261027-B07',(SELECT identity_value FROM @FixtureIds WHERE entity=N'observation' AND fixture_key=N'N-B07-1'),24);

INSERT dbo.[InventoryAdjustment] ([adjustment_id],[adjustment_date],[reason])
VALUES (N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY',N'2026-10-27',N'[DEMO] Selected recount results; only Adjustment posts the difference.');

INSERT dbo.[CountAdjustment] ([count_id],[adjustment_id])
VALUES (N'DEMO-20261027-COUNT-HISTORY',N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY');

INSERT dbo.[AdjustmentBook] ([adjustment_id],[book_id],[quantity_change])
VALUES (N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY',N'DEMO-20261027-B01',-1);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY-B01',N'DEMO-20261027-B01',-1,N'2026-10-27T04:10:00');

INSERT dbo.[AdjustmentBook] ([adjustment_id],[book_id],[quantity_change])
VALUES (N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY',N'DEMO-20261027-B03',-1);

INSERT dbo.[InventoryMovement] ([posting_key],[book_id],[quantity_delta],[occurred_at_utc])
VALUES (N'DEMO-20261027-ADJUSTMENT-COUNT-HISTORY-B03',N'DEMO-20261027-B03',-1,N'2026-10-27T04:10:00');

INSERT dbo.[InventoryAudit] ([audit_id],[audit_date],[status])
VALUES (N'DEMO-20261027-AUDIT-1',N'2026-10-27',N'APPROVED');

INSERT dbo.[InventoryAuditLine] ([audit_id],[book_id],[counted_quantity]) OUTPUT N'audit_line',N'1',inserted.[audit_line_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-AUDIT-1',N'DEMO-20261027-B01',40);

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'ADMIN',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-ADMIN',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/admin/accounts',1,N'127.0.0.1');

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'MANAGER',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-MANAGER',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/catalog/prices',1,N'127.0.0.1');

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'INVENTORY',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-INVENTORY',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/inventory/counts',1,N'127.0.0.1');

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'SALES-A',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-SALES-A',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/sales/new',1,N'127.0.0.1');

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'SALES-B',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-SALES-B',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/returns/new',0,N'127.0.0.1');

INSERT dbo.[UserAccessLog] ([account_id],[occurred_at_utc],[request_path],[was_authorized],[client_ip]) OUTPUT N'access_log',N'DISABLED',inserted.[access_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'DEMO-20261027-ACC-DISABLED',N'2026-10-27T05:00:00',N'/__demo/swd392/20261027/sales/new',0,N'127.0.0.1');

INSERT dbo.[ExternalServiceLog] ([occurred_at_utc],[service_name],[operation_name],[succeeded]) OUTPUT N'service_log',N'VnPay',inserted.[service_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'2026-10-27T05:00:00',N'DEMO/SWD392/VnPay',N'fixture-only:no-network:qr-status',1);

INSERT dbo.[ExternalServiceLog] ([occurred_at_utc],[service_name],[operation_name],[succeeded]) OUTPUT N'service_log',N'GoogleEmail',inserted.[service_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'2026-10-27T05:00:00',N'DEMO/SWD392/GoogleEmail',N'fixture-only:no-network:prepare-po-email',1);

INSERT dbo.[ExternalServiceLog] ([occurred_at_utc],[service_name],[operation_name],[succeeded]) OUTPUT N'service_log',N'ViettelPost',inserted.[service_log_id] INTO @FixtureIds(entity,fixture_key,identity_value)
VALUES (N'2026-10-27T05:00:00',N'DEMO/SWD392/ViettelPost',N'fixture-only:no-network:shipment-status',1);

INSERT dbo.BookStock(book_id,current_quantity)
SELECT b.book_id,COALESCE(SUM(m.quantity_delta),CONVERT(decimal(38,10),0))
FROM dbo.Book b LEFT JOIN dbo.InventoryMovement m ON m.book_id=b.book_id
WHERE b.book_id LIKE N'DEMO-20261027-%' GROUP BY b.book_id;

DECLARE @ExpectedStock TABLE(book_id nvarchar(200) PRIMARY KEY,quantity decimal(38,10));
INSERT @ExpectedStock VALUES (N'DEMO-20261027-B01',43),(N'DEMO-20261027-B02',27),(N'DEMO-20261027-B03',18),(N'DEMO-20261027-B04',13),(N'DEMO-20261027-B05',8),(N'DEMO-20261027-B06',12),(N'DEMO-20261027-B07',24),(N'DEMO-20261027-B08',5),(N'DEMO-20261027-B09',18),(N'DEMO-20261027-B10',14),(N'DEMO-20261027-B11',0),(N'DEMO-20261027-B12',1);

IF EXISTS(SELECT 1 FROM @ExpectedStock e LEFT JOIN dbo.BookStock s ON s.book_id=e.book_id WHERE s.book_id IS NULL OR s.current_quantity<>e.quantity) THROW 51204,'Demo final stock differs from the independently specified baseline.',1;

IF EXISTS(SELECT 1 FROM dbo.vStockReconciliation WHERE book_id LIKE N'DEMO-20261027-%' AND difference<>0) THROW 51205,'Demo stock does not reconcile.',1;

EXEC sys.sp_addextendedproperty @name=N'SWD392_Demo_20261027_v1',@value=N'caa111a435f7f2121d758df77eaa8159a10e85f5122b7cc2bc33469d2716b96f';

COMMIT TRANSACTION;
SELECT N'SEEDED' AS seed_status;
SELECT entity,fixture_key,identity_value FROM @FixtureIds ORDER BY entity,fixture_key;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
