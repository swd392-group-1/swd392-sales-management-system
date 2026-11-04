-- PROPOSAL — not yet approved. Fresh review database only; not an in-place migration.


USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THROW 51001, 'Target is not empty.', 1;
GO

BEGIN TRANSACTION;

-- One primary role confirmed. Placement on Employee is the normalization proposal; reconcile the PO note about EmployeePermission.

CREATE TABLE [dbo].[Employee] (
    [employee_id] nvarchar(200) NOT NULL,
    [name] nvarchar(max) NULL,
    [role_code] nvarchar(32) NOT NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY ([employee_id]),
    CONSTRAINT [CK_Employee_1] CHECK ([role_code] IN (N'SYSTEM_ADMIN',N'STORE_MANAGER',N'INVENTORY_STAFF',N'SALES_STAFF'))
);

-- One personal account per employee. FK+UNIQUE ensures at-most-one, not total Employee participation. password_hash stores only an application-generated password hash; NULL means credentials are not provisioned and Login must reject the account. No plaintext/default password. Login implementation remains with the module owner.

CREATE TABLE [dbo].[Account] (
    [account_id] nvarchar(200) NOT NULL,
    [employee_id] nvarchar(200) NOT NULL,
    [username] nvarchar(200) NOT NULL,
    [status] nvarchar(max) NULL,
    [password_hash] nvarchar(512) NULL,
    CONSTRAINT [PK_Account] PRIMARY KEY ([account_id]),
    CONSTRAINT [UQ_Account_1] UNIQUE ([employee_id]),
    CONSTRAINT [UQ_Account_2] UNIQUE ([username])
);

-- Permission ID is the proposed stable capability identifier. No seed grant matrix is invented.

CREATE TABLE [dbo].[Permission] (
    [permission_id] nvarchar(200) NOT NULL,
    [permission_name] nvarchar(max) NULL,
    CONSTRAINT [PK_Permission] PRIMARY KEY ([permission_id])
);

-- Only individual grants; primary role and names are not repeated here.

CREATE TABLE [dbo].[EmployeePermission] (
    [employee_id] nvarchar(200) NOT NULL,
    [permission_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_EmployeePermission] PRIMARY KEY ([employee_id], [permission_id])
);

-- PO requires page/denied-access/IP logs. Identity/time columns are proposed; unauthenticated requests may have no account. Retention/redaction open.

CREATE TABLE [dbo].[UserAccessLog] (
    [access_log_id] bigint IDENTITY(1,1) NOT NULL,
    [account_id] nvarchar(200) NULL,
    [occurred_at_utc] datetime2(7) NOT NULL,
    [request_path] nvarchar(2048) NOT NULL,
    [was_authorized] bit NOT NULL,
    [client_ip] nvarchar(45) NOT NULL,
    CONSTRAINT [PK_UserAccessLog] PRIMARY KEY ([access_log_id])
);

-- PO requires external-service usage logs; minimal fields are proposed. No credentials or message payloads.

CREATE TABLE [dbo].[ExternalServiceLog] (
    [service_log_id] bigint IDENTITY(1,1) NOT NULL,
    [occurred_at_utc] datetime2(7) NOT NULL,
    [service_name] nvarchar(100) NOT NULL,
    [operation_name] nvarchar(200) NOT NULL,
    [succeeded] bit NOT NULL,
    CONSTRAINT [PK_ExternalServiceLog] PRIMARY KEY ([service_log_id])
);

-- Source entity attributes retained for review.

CREATE TABLE [dbo].[Customer] (
    [customer_id] nvarchar(200) NOT NULL,
    [name] nvarchar(max) NULL,
    CONSTRAINT [PK_Customer] PRIMARY KEY ([customer_id])
);

-- Proposed atomic contact entries replace ambiguous contact_information. One value per row; type vocabulary/validation remain open. No copied Customer name.

CREATE TABLE [dbo].[CustomerContact] (
    [contact_id] bigint IDENTITY(1,1) NOT NULL,
    [customer_id] nvarchar(200) NOT NULL,
    [contact_kind] nvarchar(50) NOT NULL,
    [contact_value] nvarchar(1000) NOT NULL,
    CONSTRAINT [PK_CustomerContact] PRIMARY KEY ([contact_id])
);

-- Source entity attributes retained for review.

CREATE TABLE [dbo].[Provider] (
    [provider_id] nvarchar(200) NOT NULL,
    [name] nvarchar(max) NULL,
    CONSTRAINT [PK_Provider] PRIMARY KEY ([provider_id])
);

-- Proposed atomic contact entries. Original export has zero business rows at the inspected time; no unreviewed string-splitting migration.

CREATE TABLE [dbo].[ProviderContact] (
    [contact_id] bigint IDENTITY(1,1) NOT NULL,
    [provider_id] nvarchar(200) NOT NULL,
    [contact_kind] nvarchar(50) NOT NULL,
    [contact_value] nvarchar(1000) NOT NULL,
    CONSTRAINT [PK_ProviderContact] PRIMARY KEY ([contact_id])
);

-- Source catalog attributes retained. Transaction price capture timing and price-change policies remain open.

CREATE TABLE [dbo].[Book] (
    [book_id] nvarchar(200) NOT NULL,
    [title] nvarchar(max) NULL,
    [current_price] decimal(19,4) NULL,
    CONSTRAINT [PK_Book] PRIMARY KEY ([book_id])
);

-- PROPOSED typed discount value plus an explicit unit/kind replaces opaque source text. No allowed kind list, percentage bounds or stacking formula is assumed.

CREATE TABLE [dbo].[Promotion] (
    [promotion_id] nvarchar(200) NOT NULL,
    [name] nvarchar(max) NULL,
    [discount_kind] nvarchar(50) NULL,
    [discount_value] decimal(19,4) NULL,
    [start_date] date NULL,
    [end_date] date NULL,
    CONSTRAINT [PK_Promotion] PRIMARY KEY ([promotion_id])
);

-- Source pair relationship retained for review; exact cardinality/overlap rules remain open.

CREATE TABLE [dbo].[PromotionBook] (
    [promotion_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_PromotionBook] PRIMARY KEY ([promotion_id], [book_id])
);

-- Source total retained as recorded transaction total; discount allocation/cost/profit rules are not invented.

CREATE TABLE [dbo].[SalesOrder] (
    [order_id] nvarchar(200) NOT NULL,
    [order_date] date NULL,
    [total_order_amount] decimal(19,4) NULL,
    [customer_id] nvarchar(200) NULL,
    CONSTRAINT [PK_SalesOrder] PRIMARY KEY ([order_id])
);

-- Replaces OrderBook pair PK. Repeated Book lines supported; line identity is a technical proposal. Unit price is source transaction data, not a new snapshot-timing rule.

CREATE TABLE [dbo].[SalesOrderLine] (
    [order_line_id] bigint IDENTITY(1,1) NOT NULL,
    [order_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [quantity] decimal(38,10) NOT NULL,
    [unit_price] decimal(19,4) NULL,
    CONSTRAINT [PK_SalesOrderLine] PRIMARY KEY ([order_line_id]),
    CONSTRAINT [CK_SalesOrderLine_1] CHECK ([quantity] > 0)
);

-- Source fields retained. Settlement amount, attempts, allocation, QR callbacks and idempotency rules remain open.

CREATE TABLE [dbo].[Payment] (
    [payment_id] nvarchar(200) NOT NULL,
    [payment_method] nvarchar(max) NULL,
    [payment_status] nvarchar(max) NULL,
    CONSTRAINT [PK_Payment] PRIMARY KEY ([payment_id])
);

-- PROVISIONAL source link, not confirmation that payments may cover multiple orders. Revisit when payment cardinality is decided.

CREATE TABLE [dbo].[OrderPayment] (
    [order_id] nvarchar(200) NOT NULL,
    [payment_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_OrderPayment] PRIMARY KEY ([order_id], [payment_id])
);

-- Confirmed: one original order per return, multiple returns per order. Cash-only refund is a policy, not a repeated constant method column.

CREATE TABLE [dbo].[BookReturn] (
    [return_id] nvarchar(200) NOT NULL,
    [original_order_id] nvarchar(200) NOT NULL,
    [returned_at_utc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_BookReturn] PRIMARY KEY ([return_id])
);

-- Do not repeat original_order_id/book_id/unit_price: the referenced sales line determines them. Trigger enforces same original order; cumulative quantity uses transactional validation. Eligibility/refund allocation remain open.

CREATE TABLE [dbo].[BookReturnLine] (
    [return_line_id] bigint IDENTITY(1,1) NOT NULL,
    [return_id] nvarchar(200) NOT NULL,
    [original_order_line_id] bigint NOT NULL,
    [returned_quantity] decimal(38,10) NOT NULL,
    [refund_amount] decimal(19,4) NOT NULL,
    CONSTRAINT [PK_BookReturnLine] PRIMARY KEY ([return_line_id]),
    CONSTRAINT [CK_BookReturnLine_1] CHECK ([returned_quantity] > 0),
    CONSTRAINT [CK_BookReturnLine_2] CHECK ([refund_amount] >= 0)
);

-- Source entity attributes retained for review.

CREATE TABLE [dbo].[PurchaseOrder] (
    [purchase_order_id] nvarchar(200) NOT NULL,
    [order_date] date NULL,
    [status] nvarchar(max) NULL,
    CONSTRAINT [PK_PurchaseOrder] PRIMARY KEY ([purchase_order_id])
);

-- Replaces PurchaseOrderBook pair key; repeated Book lines allowed. No inferred purchase cost or line price.

CREATE TABLE [dbo].[PurchaseOrderLine] (
    [purchase_order_line_id] bigint IDENTITY(1,1) NOT NULL,
    [purchase_order_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [ordered_quantity] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderLine] PRIMARY KEY ([purchase_order_line_id]),
    CONSTRAINT [CK_PurchaseOrderLine_1] CHECK ([ordered_quantity] > 0)
);

-- PROVISIONAL source link; one-versus-multiple providers per PO remains open. Not a newly confirmed M:N business rule.

CREATE TABLE [dbo].[PurchaseOrderProvider] (
    [purchase_order_id] nvarchar(200) NOT NULL,
    [provider_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderProvider] PRIMARY KEY ([purchase_order_id], [provider_id])
);

-- Confirmed multi-PO receipt: no purchase_order_id on header; PO membership derives from receipt lines.

CREATE TABLE [dbo].[GoodsReceipt] (
    [receipt_id] nvarchar(200) NOT NULL,
    [receipt_date] date NULL,
    CONSTRAINT [PK_GoodsReceipt] PRIMARY KEY ([receipt_id])
);

-- References exact PO line. Do not repeat PO ID or Book ID: they depend on purchase_order_line_id. Multiple POs per receipt and multiple receipts per PO supported.

CREATE TABLE [dbo].[GoodsReceiptLine] (
    [receipt_line_id] bigint IDENTITY(1,1) NOT NULL,
    [receipt_id] nvarchar(200) NOT NULL,
    [purchase_order_line_id] bigint NOT NULL,
    [received_quantity] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_GoodsReceiptLine] PRIMARY KEY ([receipt_line_id]),
    CONSTRAINT [CK_GoodsReceiptLine_1] CHECK ([received_quantity] > 0)
);

-- Technical shipment ID avoids assuming tracking numbers are globally unique. Carrier namespace/reuse rules remain open; no tracking-number UNIQUE is asserted.

CREATE TABLE [dbo].[Shipment] (
    [shipment_id] bigint IDENTITY(1,1) NOT NULL,
    [tracking_number] nvarchar(200) NOT NULL,
    [shipment_status] nvarchar(max) NULL,
    CONSTRAINT [PK_Shipment] PRIMARY KEY ([shipment_id])
);

-- PROVISIONAL source pair relation.

CREATE TABLE [dbo].[ProviderShipment] (
    [provider_id] nvarchar(200) NOT NULL,
    [shipment_id] bigint NOT NULL,
    CONSTRAINT [PK_ProviderShipment] PRIMARY KEY ([provider_id], [shipment_id])
);

-- PROVISIONAL source pair relation.

CREATE TABLE [dbo].[PurchaseOrderShipment] (
    [purchase_order_id] nvarchar(200) NOT NULL,
    [shipment_id] bigint NOT NULL,
    CONSTRAINT [PK_PurchaseOrderShipment] PRIMARY KEY ([purchase_order_id], [shipment_id])
);

-- Source pair retained. Role authorization and repeated approval/history semantics are not implemented by this link.

CREATE TABLE [dbo].[PurchaseOrderApproval] (
    [employee_id] nvarchar(200) NOT NULL,
    [purchase_order_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderApproval] PRIMARY KEY ([employee_id], [purchase_order_id])
);

-- Audit/approval does not update stock or automatically create Adjustment; no trigger does so.

CREATE TABLE [dbo].[InventoryAudit] (
    [audit_id] nvarchar(200) NOT NULL,
    [audit_date] date NULL,
    [status] nvarchar(max) NULL,
    CONSTRAINT [PK_InventoryAudit] PRIMARY KEY ([audit_id])
);

-- Proposed observation identity; Audit observation/recount grain remains under review, distinct from independent Count.

CREATE TABLE [dbo].[InventoryAuditLine] (
    [audit_line_id] bigint IDENTITY(1,1) NOT NULL,
    [audit_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [counted_quantity] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_InventoryAuditLine] PRIMARY KEY ([audit_line_id]),
    CONSTRAINT [CK_InventoryAuditLine_1] CHECK ([counted_quantity] >= 0)
);

-- Independent Count, not an Audit or its approval.

CREATE TABLE [dbo].[InventoryCount] (
    [count_id] nvarchar(200) NOT NULL,
    [counted_at_utc] datetime2(7) NOT NULL,
    [recorded_by_employee_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_InventoryCount] PRIMARY KEY ([count_id])
);

-- Confirmed repeated Book rows mean recounts. Each observation is retained, never summed as independent stock. Extra triple UNIQUE exists only as a composite FK target, not a minimal candidate key.

CREATE TABLE [dbo].[InventoryCountObservation] (
    [observation_id] bigint IDENTITY(1,1) NOT NULL,
    [count_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [counted_quantity] decimal(38,10) NOT NULL,
    [observed_at_utc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_InventoryCountObservation] PRIMARY KEY ([observation_id]),
    CONSTRAINT [UQ_InventoryCountObservation_1] UNIQUE ([observation_id], [count_id], [book_id]),
    CONSTRAINT [CK_InventoryCountObservation_1] CHECK ([counted_quantity] >= 0)
);

-- One chosen final observation per Count/Book; it need not be MAX(time). Composite FK proves selected observation matches that Count and Book. recorded_quantity is the historical comparison baseline, not current BookStock.

CREATE TABLE [dbo].[InventoryCountSelection] (
    [count_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [selected_observation_id] bigint NOT NULL,
    [recorded_quantity] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_InventoryCountSelection] PRIMARY KEY ([count_id], [book_id]),
    CONSTRAINT [UQ_InventoryCountSelection_1] UNIQUE ([selected_observation_id])
);

-- quantity_change moved off header to per-Book line. Manual adjustment semantics remain open.

CREATE TABLE [dbo].[InventoryAdjustment] (
    [adjustment_id] nvarchar(200) NOT NULL,
    [adjustment_date] date NULL,
    [reason] nvarchar(max) NULL,
    CONSTRAINT [PK_InventoryAdjustment] PRIMARY KEY ([adjustment_id])
);

-- One delta per Book per Adjustment, proposed grain supported by one Adjustment containing differing Books of Count.

CREATE TABLE [dbo].[AdjustmentBook] (
    [adjustment_id] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [quantity_change] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_AdjustmentBook] PRIMARY KEY ([adjustment_id], [book_id])
);

-- At most one resulting Adjustment per Count and one source Count per automatic Adjustment. No link is needed if Count has no differences.

CREATE TABLE [dbo].[CountAdjustment] (
    [count_id] nvarchar(200) NOT NULL,
    [adjustment_id] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_CountAdjustment] PRIMARY KEY ([count_id]),
    CONSTRAINT [UQ_CountAdjustment_1] UNIQUE ([adjustment_id])
);

-- Confirmed stored current stock. No nonnegative-stock or opening balance is assumed. No automated posting workflow is created by this table.

CREATE TABLE [dbo].[BookStock] (
    [book_id] nvarchar(200) NOT NULL,
    [current_quantity] decimal(38,10) NOT NULL,
    CONSTRAINT [PK_BookStock] PRIMARY KEY ([book_id])
);

-- Proposed immutable movement history; opaque stable posting_key supports idempotency. It is not a polymorphic FK or a guarantee of source-document consistency. Posting+BookStock must be atomic in future application code.

CREATE TABLE [dbo].[InventoryMovement] (
    [movement_id] bigint IDENTITY(1,1) NOT NULL,
    [posting_key] nvarchar(200) NOT NULL,
    [book_id] nvarchar(200) NOT NULL,
    [quantity_delta] decimal(38,10) NOT NULL,
    [occurred_at_utc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_InventoryMovement] PRIMARY KEY ([movement_id]),
    CONSTRAINT [UQ_InventoryMovement_1] UNIQUE ([posting_key])
);

ALTER TABLE [dbo].[Account] WITH CHECK ADD CONSTRAINT [FK_Account_1] FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee] ([employee_id]);

ALTER TABLE [dbo].[EmployeePermission] WITH CHECK ADD CONSTRAINT [FK_EmployeePermission_1] FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee] ([employee_id]);

ALTER TABLE [dbo].[EmployeePermission] WITH CHECK ADD CONSTRAINT [FK_EmployeePermission_2] FOREIGN KEY ([permission_id]) REFERENCES [dbo].[Permission] ([permission_id]);

ALTER TABLE [dbo].[UserAccessLog] WITH CHECK ADD CONSTRAINT [FK_UserAccessLog_1] FOREIGN KEY ([account_id]) REFERENCES [dbo].[Account] ([account_id]);

ALTER TABLE [dbo].[CustomerContact] WITH CHECK ADD CONSTRAINT [FK_CustomerContact_1] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer] ([customer_id]);

ALTER TABLE [dbo].[ProviderContact] WITH CHECK ADD CONSTRAINT [FK_ProviderContact_1] FOREIGN KEY ([provider_id]) REFERENCES [dbo].[Provider] ([provider_id]);

ALTER TABLE [dbo].[PromotionBook] WITH CHECK ADD CONSTRAINT [FK_PromotionBook_1] FOREIGN KEY ([promotion_id]) REFERENCES [dbo].[Promotion] ([promotion_id]);

ALTER TABLE [dbo].[PromotionBook] WITH CHECK ADD CONSTRAINT [FK_PromotionBook_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[SalesOrder] WITH CHECK ADD CONSTRAINT [FK_SalesOrder_1] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer] ([customer_id]);

ALTER TABLE [dbo].[SalesOrderLine] WITH CHECK ADD CONSTRAINT [FK_SalesOrderLine_1] FOREIGN KEY ([order_id]) REFERENCES [dbo].[SalesOrder] ([order_id]);

ALTER TABLE [dbo].[SalesOrderLine] WITH CHECK ADD CONSTRAINT [FK_SalesOrderLine_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[OrderPayment] WITH CHECK ADD CONSTRAINT [FK_OrderPayment_1] FOREIGN KEY ([order_id]) REFERENCES [dbo].[SalesOrder] ([order_id]);

ALTER TABLE [dbo].[OrderPayment] WITH CHECK ADD CONSTRAINT [FK_OrderPayment_2] FOREIGN KEY ([payment_id]) REFERENCES [dbo].[Payment] ([payment_id]);

ALTER TABLE [dbo].[BookReturn] WITH CHECK ADD CONSTRAINT [FK_BookReturn_1] FOREIGN KEY ([original_order_id]) REFERENCES [dbo].[SalesOrder] ([order_id]);

ALTER TABLE [dbo].[BookReturnLine] WITH CHECK ADD CONSTRAINT [FK_BookReturnLine_1] FOREIGN KEY ([return_id]) REFERENCES [dbo].[BookReturn] ([return_id]);

ALTER TABLE [dbo].[BookReturnLine] WITH CHECK ADD CONSTRAINT [FK_BookReturnLine_2] FOREIGN KEY ([original_order_line_id]) REFERENCES [dbo].[SalesOrderLine] ([order_line_id]);

ALTER TABLE [dbo].[PurchaseOrderLine] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderLine_1] FOREIGN KEY ([purchase_order_id]) REFERENCES [dbo].[PurchaseOrder] ([purchase_order_id]);

ALTER TABLE [dbo].[PurchaseOrderLine] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderLine_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[PurchaseOrderProvider] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderProvider_1] FOREIGN KEY ([purchase_order_id]) REFERENCES [dbo].[PurchaseOrder] ([purchase_order_id]);

ALTER TABLE [dbo].[PurchaseOrderProvider] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderProvider_2] FOREIGN KEY ([provider_id]) REFERENCES [dbo].[Provider] ([provider_id]);

ALTER TABLE [dbo].[GoodsReceiptLine] WITH CHECK ADD CONSTRAINT [FK_GoodsReceiptLine_1] FOREIGN KEY ([receipt_id]) REFERENCES [dbo].[GoodsReceipt] ([receipt_id]);

ALTER TABLE [dbo].[GoodsReceiptLine] WITH CHECK ADD CONSTRAINT [FK_GoodsReceiptLine_2] FOREIGN KEY ([purchase_order_line_id]) REFERENCES [dbo].[PurchaseOrderLine] ([purchase_order_line_id]);

ALTER TABLE [dbo].[ProviderShipment] WITH CHECK ADD CONSTRAINT [FK_ProviderShipment_1] FOREIGN KEY ([provider_id]) REFERENCES [dbo].[Provider] ([provider_id]);

ALTER TABLE [dbo].[ProviderShipment] WITH CHECK ADD CONSTRAINT [FK_ProviderShipment_2] FOREIGN KEY ([shipment_id]) REFERENCES [dbo].[Shipment] ([shipment_id]);

ALTER TABLE [dbo].[PurchaseOrderShipment] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderShipment_1] FOREIGN KEY ([purchase_order_id]) REFERENCES [dbo].[PurchaseOrder] ([purchase_order_id]);

ALTER TABLE [dbo].[PurchaseOrderShipment] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderShipment_2] FOREIGN KEY ([shipment_id]) REFERENCES [dbo].[Shipment] ([shipment_id]);

ALTER TABLE [dbo].[PurchaseOrderApproval] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderApproval_1] FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee] ([employee_id]);

ALTER TABLE [dbo].[PurchaseOrderApproval] WITH CHECK ADD CONSTRAINT [FK_PurchaseOrderApproval_2] FOREIGN KEY ([purchase_order_id]) REFERENCES [dbo].[PurchaseOrder] ([purchase_order_id]);

ALTER TABLE [dbo].[InventoryAuditLine] WITH CHECK ADD CONSTRAINT [FK_InventoryAuditLine_1] FOREIGN KEY ([audit_id]) REFERENCES [dbo].[InventoryAudit] ([audit_id]);

ALTER TABLE [dbo].[InventoryAuditLine] WITH CHECK ADD CONSTRAINT [FK_InventoryAuditLine_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[InventoryCount] WITH CHECK ADD CONSTRAINT [FK_InventoryCount_1] FOREIGN KEY ([recorded_by_employee_id]) REFERENCES [dbo].[Employee] ([employee_id]);

ALTER TABLE [dbo].[InventoryCountObservation] WITH CHECK ADD CONSTRAINT [FK_InventoryCountObservation_1] FOREIGN KEY ([count_id]) REFERENCES [dbo].[InventoryCount] ([count_id]);

ALTER TABLE [dbo].[InventoryCountObservation] WITH CHECK ADD CONSTRAINT [FK_InventoryCountObservation_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[InventoryCountSelection] WITH CHECK ADD CONSTRAINT [FK_InventoryCountSelection_1] FOREIGN KEY ([selected_observation_id], [count_id], [book_id]) REFERENCES [dbo].[InventoryCountObservation] ([observation_id], [count_id], [book_id]);

ALTER TABLE [dbo].[AdjustmentBook] WITH CHECK ADD CONSTRAINT [FK_AdjustmentBook_1] FOREIGN KEY ([adjustment_id]) REFERENCES [dbo].[InventoryAdjustment] ([adjustment_id]);

ALTER TABLE [dbo].[AdjustmentBook] WITH CHECK ADD CONSTRAINT [FK_AdjustmentBook_2] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[CountAdjustment] WITH CHECK ADD CONSTRAINT [FK_CountAdjustment_1] FOREIGN KEY ([count_id]) REFERENCES [dbo].[InventoryCount] ([count_id]);

ALTER TABLE [dbo].[CountAdjustment] WITH CHECK ADD CONSTRAINT [FK_CountAdjustment_2] FOREIGN KEY ([adjustment_id]) REFERENCES [dbo].[InventoryAdjustment] ([adjustment_id]);

ALTER TABLE [dbo].[BookStock] WITH CHECK ADD CONSTRAINT [FK_BookStock_1] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

ALTER TABLE [dbo].[InventoryMovement] WITH CHECK ADD CONSTRAINT [FK_InventoryMovement_1] FOREIGN KEY ([book_id]) REFERENCES [dbo].[Book] ([book_id]);

COMMIT TRANSACTION;
GO
