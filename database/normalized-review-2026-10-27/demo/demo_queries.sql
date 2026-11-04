-- Book Sales Management System: read-only walkthrough of fictional demo data.
USE [SWD392_BookSales_3NF_REVIEW_20261027];
GO
SET NOCOUNT ON;

-- Demo account records, not SQL logins and not working application credentials.
SELECT a.username,a.status,e.name,e.role_code,COUNT(p.permission_id) AS stored_demo_grants
FROM dbo.Account a JOIN dbo.Employee e ON e.employee_id=a.employee_id
LEFT JOIN dbo.EmployeePermission p ON p.employee_id=e.employee_id
WHERE e.employee_id LIKE N'DEMO-20261027-%'
GROUP BY a.username,a.status,e.name,e.role_code ORDER BY a.username;

-- Nam / UC10: baseline price is 130000; changing it is a future application action.
SELECT book_id,title,current_price FROM dbo.Book WHERE book_id=N'DEMO-20261027-B01';

-- Kiet / UC07: two recount observations (11,12), current stock 13, no selection yet.
SELECT c.count_id,o.observation_id,o.book_id,o.counted_quantity,s.current_quantity,o.observed_at_utc
FROM dbo.InventoryCount c JOIN dbo.InventoryCountObservation o ON o.count_id=c.count_id
JOIN dbo.BookStock s ON s.book_id=o.book_id
WHERE c.count_id=N'DEMO-20261027-COUNT-READY' ORDER BY o.observation_id;

-- Applied history example: chosen results, one Adjustment with two differing Books.
SELECT s.*,a.adjustment_id,l.quantity_change
FROM dbo.vSelectedInventoryCount s
LEFT JOIN dbo.CountAdjustment a ON a.count_id=s.count_id
LEFT JOIN dbo.AdjustmentBook l ON l.adjustment_id=a.adjustment_id AND l.book_id=s.book_id
WHERE s.count_id LIKE N'DEMO-20261027-%' ORDER BY s.count_id,s.book_id;

-- Hung / UC01: records available for a new Cash sale; B11=0, B12=1.
SELECT b.book_id,b.title,b.current_price,s.current_quantity
FROM dbo.Book b JOIN dbo.BookStock s ON s.book_id=b.book_id
WHERE b.book_id LIKE N'DEMO-20261027-%' ORDER BY b.book_id;

-- Return example: original sales-line identity matters even when Book is repeated.
SELECT l.order_id,l.order_line_id,l.book_id,l.quantity AS purchased_quantity,l.unit_price,
       COALESCE(r.returned_quantity,CONVERT(decimal(38,10),0)) AS already_returned_quantity,
       remaining.remaining_quantity
FROM dbo.SalesOrderLine l
LEFT JOIN (SELECT original_order_line_id,SUM(returned_quantity) AS returned_quantity
           FROM dbo.BookReturnLine GROUP BY original_order_line_id) r ON r.original_order_line_id=l.order_line_id
CROSS APPLY (SELECT SUM(v.quantity) AS remaining_quantity
             FROM (VALUES(l.quantity),(-COALESCE(r.returned_quantity,CONVERT(decimal(38,10),0)))) v(quantity)) remaining
WHERE l.order_id=N'DEMO-20261027-SALE-1' ORDER BY l.order_line_id;
SELECT * FROM dbo.vBookReturnDetails WHERE return_id LIKE N'DEMO-20261027-%' ORDER BY return_id,return_line_id;

-- Hùng / UC12: Sales A has a stored Return grant; Sales B lacks it.
-- This describes fixture rows, not actual HTTP authorization behavior.
SELECT a.username,a.status,e.role_code,
       CONVERT(bit,CASE WHEN g.permission_id IS NULL THEN 0 ELSE 1 END) AS has_demo_return_grant
FROM dbo.Account a JOIN dbo.Employee e ON e.employee_id=a.employee_id
LEFT JOIN dbo.EmployeePermission g ON g.employee_id=e.employee_id AND g.permission_id=N'DEMO-20261027-UC21'
WHERE e.employee_id IN(N'DEMO-20261027-EMP-SALES-A',N'DEMO-20261027-EMP-SALES-B',N'DEMO-20261027-EMP-DISABLED')
ORDER BY a.username;

-- Tech Lead / UC15: catalog-price display value, not cost, profit or accounting valuation.
SELECT b.book_id,b.title,b.current_price,s.current_quantity,b.current_price*s.current_quantity AS catalog_display_value
FROM dbo.Book b JOIN dbo.BookStock s ON s.book_id=b.book_id
WHERE b.book_id LIKE N'DEMO-20261027-%' ORDER BY b.book_id;
SELECT COUNT(*) AS book_count,SUM(s.current_quantity) AS stock_units,
       SUM(b.current_price*s.current_quantity) AS catalog_display_value
FROM dbo.Book b JOIN dbo.BookStock s ON s.book_id=b.book_id WHERE b.book_id LIKE N'DEMO-20261027-%';

-- Receiving: GR-1 combines two POs; PO-1 spans GR-1 and GR-2.
SELECT * FROM dbo.vGoodsReceiptSources WHERE receipt_id LIKE N'DEMO-20261027-%' ORDER BY receipt_id,receipt_line_id;

-- Every demo BookStock balance must match its synthetic opening/receipt/sale/return/adjustment movements.
SELECT * FROM dbo.vStockReconciliation WHERE book_id LIKE N'DEMO-20261027-%' ORDER BY book_id;
GO
