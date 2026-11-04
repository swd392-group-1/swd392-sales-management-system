-- PROPOSAL — not yet approved. SQL integrity support, not a complete Return workflow.
USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER VIEW dbo.vGoodsReceiptSources
AS
SELECT r.receipt_id, l.receipt_line_id, p.purchase_order_id,
       p.purchase_order_line_id, p.book_id, l.received_quantity
FROM dbo.GoodsReceipt AS r
JOIN dbo.GoodsReceiptLine AS l ON l.receipt_id=r.receipt_id
JOIN dbo.PurchaseOrderLine AS p ON p.purchase_order_line_id=l.purchase_order_line_id;
GO
CREATE OR ALTER VIEW dbo.vSelectedInventoryCount
AS
SELECT s.count_id, s.book_id, s.selected_observation_id, s.recorded_quantity,
       o.counted_quantity, d.quantity_difference
FROM dbo.InventoryCountSelection AS s
JOIN dbo.InventoryCountObservation AS o ON o.observation_id=s.selected_observation_id
CROSS APPLY (
    -- SUM retains decimal(38,10); direct subtraction can reduce scale to 9.
    SELECT SUM(v.quantity) AS quantity_difference
    FROM (VALUES (o.counted_quantity),(-s.recorded_quantity)) AS v(quantity)
) AS d;
GO
CREATE OR ALTER VIEW dbo.vBookReturnDetails
AS
SELECT r.return_id, r.original_order_id, r.returned_at_utc,
       l.return_line_id, l.original_order_line_id, s.book_id,
       l.returned_quantity, l.refund_amount
FROM dbo.BookReturn AS r
JOIN dbo.BookReturnLine AS l ON l.return_id=r.return_id
JOIN dbo.SalesOrderLine AS s ON s.order_line_id=l.original_order_line_id;
GO
CREATE OR ALTER VIEW dbo.vStockReconciliation
AS
SELECT s.book_id, s.current_quantity,
       COALESCE(m.ledger_quantity,CONVERT(decimal(38,10),0)) AS ledger_quantity,
       d.difference
FROM dbo.BookStock AS s
LEFT JOIN (
    SELECT book_id,SUM(quantity_delta) AS ledger_quantity
    FROM dbo.InventoryMovement GROUP BY book_id
) AS m ON m.book_id=s.book_id
CROSS APPLY (
    SELECT SUM(v.quantity) AS difference
    FROM (VALUES (s.current_quantity),(-COALESCE(m.ledger_quantity,CONVERT(decimal(38,10),0)))) AS v(quantity)
) AS d;
GO
-- BookReturn/BookReturnLine represent accepted returns. Draft/rejected requests are not modeled.
-- The parent sales-line lock serializes competing return validation for that line.
-- A deadlock victim must be retried by future application transaction handling.
CREATE OR ALTER TRIGGER dbo.TR_BookReturnLine_Validate
ON dbo.BookReturnLine
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @locked bigint;
    SELECT @locked=COUNT_BIG(*)
    FROM dbo.SalesOrderLine AS s WITH (UPDLOCK,HOLDLOCK)
    WHERE s.order_line_id IN (SELECT original_order_line_id FROM inserted);

    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.BookReturn r ON r.return_id=i.return_id
        JOIN dbo.SalesOrderLine s ON s.order_line_id=i.original_order_line_id
        WHERE r.original_order_id<>s.order_id
    ) THROW 51010, 'Return line must belong to the original order of its Return.', 1;

    IF EXISTS (
        SELECT l.original_order_line_id
        FROM dbo.BookReturnLine AS l WITH (HOLDLOCK)
        JOIN dbo.SalesOrderLine s ON s.order_line_id=l.original_order_line_id
        WHERE l.original_order_line_id IN (SELECT original_order_line_id FROM inserted)
        GROUP BY l.original_order_line_id,s.quantity
        HAVING SUM(l.returned_quantity)>s.quantity
    ) THROW 51011, 'Accumulated returned quantity exceeds the original sales line.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_BookReturn_ProtectOrder
ON dbo.BookReturn
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.BookReturnLine l ON l.return_id=i.return_id
        JOIN dbo.SalesOrderLine s ON s.order_line_id=l.original_order_line_id
        WHERE i.original_order_id<>s.order_id
    ) THROW 51010, 'Changing Return order would invalidate existing return lines.', 1;
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_SalesOrderLine_ProtectReturns
ON dbo.SalesOrderLine
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN dbo.BookReturnLine l ON l.original_order_line_id=i.order_line_id
        JOIN dbo.BookReturn r ON r.return_id=l.return_id
        WHERE r.original_order_id<>i.order_id
    ) THROW 51010, 'Changing sales-line order would invalidate an existing return.', 1;
    IF EXISTS (
        SELECT l.original_order_line_id
        FROM dbo.BookReturnLine l WITH (HOLDLOCK)
        JOIN inserted i ON i.order_line_id=l.original_order_line_id
        GROUP BY l.original_order_line_id,i.quantity
        HAVING SUM(l.returned_quantity)>i.quantity
    ) THROW 51011, 'Sales quantity cannot become less than quantity already returned.', 1;
END;
GO
