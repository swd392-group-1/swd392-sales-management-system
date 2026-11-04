"""Build fictional, versioned demo fixtures. Does not connect to SQL Server."""
from pathlib import Path
from decimal import Decimal
from collections import Counter
import hashlib
import json

ROOT=Path(__file__).resolve().parent
PREFIX='DEMO-20261027-'
DATABASE='SWD392_BookSales_3NF_REVIEW_20261027'
MARKER='SWD392_Demo_20261027_v1'
def ident(s): return PREFIX+s
class Ref:
    def __init__(self,kind,key): self.kind,self.key=kind,key
    def json(self): return {'identity_ref':[self.kind,self.key]}
def ref(kind,key): return Ref(kind,key)
def literal(value):
    if isinstance(value,Ref):
        return f"(SELECT identity_value FROM @FixtureIds WHERE entity={literal(value.kind)} AND fixture_key={literal(value.key)})"
    if value is None:return 'NULL'
    if isinstance(value,bool):return '1' if value else '0'
    if isinstance(value,(int,Decimal)):return str(value)
    return "N'"+str(value).replace("'","''")+"'"
def q(name):return '['+name+']'

rows=[]
def add(table,data,identity=None): rows.append(dict(table=table,data=data,identity=identity))

employees=[('ADMIN','Quản trị viên mẫu','SYSTEM_ADMIN'),('MANAGER','Quản lý mẫu','STORE_MANAGER'),
 ('INVENTORY','Nhân viên kho mẫu','INVENTORY_STAFF'),('SALES-A','Nhân viên bán hàng mẫu A','SALES_STAFF'),
 ('SALES-B','Nhân viên bán hàng mẫu B','SALES_STAFF'),('DISABLED','Tài khoản mẫu đã khóa','SALES_STAFF')]
for key,name,role in employees:
    add('Employee',dict(employee_id=ident('EMP-'+key),name=name+' [DEMO]',role_code=role))
    add('Account',dict(account_id=ident('ACC-'+key),employee_id=ident('EMP-'+key),
        username='swd392_demo_'+key.lower().replace('-','_'),status='DISABLED' if key=='DISABLED' else 'ACTIVE'))
uc_names=['Process Sale','Process QR Payment','Maintain Book Information','Maintain Provider Information',
 'Prepare Purchase Order','Record Goods Receipt','Record Inventory Count','Record Inventory Adjustment',
 'Perform Inventory Audit','Update Price','Update Discount/Promotion','Update Staff Permissions',
 'Approve Purchase Order','Approve Inventory Audit','View Management Reports','Manage Accounts',
 'Configure System','Review System Activity Logs','Review Security Audit Logs','Monitor System Alerts','Process Book Return']
for n,name in enumerate(uc_names,1):add('Permission',dict(permission_id=ident(f'UC{n:02}'),permission_name=name+' [DEMO]'))
grants={'ADMIN':list(range(1,22)),'MANAGER':list(range(10,16)),'INVENTORY':list(range(3,10)),
        'SALES-A':[1,2,21],'SALES-B':[1],'DISABLED':[1]}
for employee,permissions in grants.items():
    for n in permissions:add('EmployeePermission',dict(employee_id=ident('EMP-'+employee),permission_id=ident(f'UC{n:02}')))

books=[('B01','Thiết kế hướng đối tượng',130000,30,43),('B02','Lập trình C# căn bản',150000,20,27),
 ('B03','Cơ sở dữ liệu thực hành',180000,15,18),('B04','Ứng dụng web ASP.NET Core',200000,10,13),
 ('B05','Mẫu thiết kế phần mềm',220000,8,8),('B06','Kiểm thử phần mềm',120000,12,12),
 ('B07','Git cho nhóm phát triển',95000,25,24),('B08','Kiến trúc phần mềm',250000,6,5),
 ('B09','Cấu trúc dữ liệu',175000,18,18),('B10','Mạng máy tính',145000,14,14),
 ('B11','Sách mẫu hết hàng',110000,0,0),('B12','Sách mẫu sắp hết',165000,1,1)]
prices={key:price for key,_,price,_,_ in books}
for key,title,price,opening,final in books:
    add('Book',dict(book_id=ident(key),title=title+' [DEMO]',current_price=price))
    if opening:
        add('InventoryMovement',dict(posting_key=ident('OPEN-'+key),book_id=ident(key),quantity_delta=opening,occurred_at_utc='2026-10-19T00:00:00'))
for key,name in [('A','Nhà cung cấp mẫu A'),('B','Nhà cung cấp mẫu B')]:
    add('Provider',dict(provider_id=ident('PROVIDER-'+key),name=name+' [DEMO]'))
    add('ProviderContact',dict(provider_id=ident('PROVIDER-'+key),contact_kind='EMAIL',contact_value=f'provider-{key.lower()}@example.test'),('provider_contact',key))
for n in range(1,4):
    add('Customer',dict(customer_id=ident(f'CUSTOMER-{n}'),name=f'Khách hàng mẫu {n} [DEMO]'))
    add('CustomerContact',dict(customer_id=ident(f'CUSTOMER-{n}'),contact_kind='EMAIL',contact_value=f'customer-{n}@example.test'),('customer_contact',str(n)))
for n in range(1,5):
    add('PurchaseOrder',dict(purchase_order_id=ident(f'PO-{n}'),order_date='2026-10-19',status='RECEIVED' if n<3 else 'APPROVED'))
    add('PurchaseOrderProvider',dict(purchase_order_id=ident(f'PO-{n}'),provider_id=ident('PROVIDER-'+('B' if n==3 else 'A'))))
    add('PurchaseOrderApproval',dict(employee_id=ident('EMP-MANAGER'),purchase_order_id=ident(f'PO-{n}')))
po_lines=[('PO1-L1',1,'B01',10),('PO1-L2',1,'B01',5),('PO1-L3',1,'B02',8),
          ('PO2-L1',2,'B03',6),('PO2-L2',2,'B04',4),('PO3-L1',3,'B05',5),('PO3-L2',3,'B11',10),('PO4-L1',4,'B12',5)]
po_book={key:book for key,_,book,_ in po_lines}
for key,po,book,qty in po_lines:
    add('PurchaseOrderLine',dict(purchase_order_id=ident(f'PO-{po}'),book_id=ident(book),ordered_quantity=qty),('po_line',key))
receipts=[('GR1-L1',1,'PO1-L1',6),('GR1-L2',1,'PO2-L1',4),
          ('GR2-L1',2,'PO1-L1',4),('GR2-L2',2,'PO1-L2',5),('GR2-L3',2,'PO1-L3',8),('GR2-L4',2,'PO2-L1',2),
          ('GR3-L1',3,'PO2-L2',4)]
for n in range(1,4):add('GoodsReceipt',dict(receipt_id=ident(f'GR-{n}'),receipt_date=f'2026-10-{19+n:02}'))
for key,receipt,po_key,qty in receipts:
    add('GoodsReceiptLine',dict(receipt_id=ident(f'GR-{receipt}'),purchase_order_line_id=ref('po_line',po_key),received_quantity=qty),('receipt_line',key))
    add('InventoryMovement',dict(posting_key=ident('RECEIPT-'+key),book_id=ident(po_book[po_key]),quantity_delta=qty,
        occurred_at_utc=f'2026-10-{19+receipt:02}T00:30:00'))
for n,provider,status in [(1,'A','DELIVERED'),(2,'B','PENDING')]:
    add('Shipment',dict(tracking_number=ident(f'TRACK-{n}'),shipment_status=status),('shipment',str(n)))
    add('ProviderShipment',dict(provider_id=ident('PROVIDER-'+provider),shipment_id=ref('shipment',str(n))))
for po,shipment in [(1,1),(2,1),(3,2)]:
    add('PurchaseOrderShipment',dict(purchase_order_id=ident(f'PO-{po}'),shipment_id=ref('shipment',str(shipment))))

sale_lines=[('SALE1-L1',1,'B01',2),('SALE1-L2',1,'B01',1),('SALE1-L3',1,'B02',1),
            ('SALE2-L1',2,'B03',2),('SALE2-L2',2,'B04',1),('SALE3-L1',3,'B07',1),('SALE3-L2',3,'B08',1)]
sale_book={key:book for key,_,book,_ in sale_lines}
sales_totals={n:sum(prices[book]*qty for _,sale,book,qty in sale_lines if sale==n) for n in range(1,4)}
for n in range(1,4):
    add('SalesOrder',dict(order_id=ident(f'SALE-{n}'),order_date=f'2026-10-{21+n:02}',total_order_amount=sales_totals[n],
        customer_id=ident(f'CUSTOMER-{n}') if n<3 else None))
    add('Payment',dict(payment_id=ident(f'PAYMENT-{n}'),payment_method='CASH',payment_status='COMPLETED'))
    add('OrderPayment',dict(order_id=ident(f'SALE-{n}'),payment_id=ident(f'PAYMENT-{n}')))
for key,sale,book,qty in sale_lines:
    add('SalesOrderLine',dict(order_id=ident(f'SALE-{sale}'),book_id=ident(book),quantity=qty,unit_price=prices[book]),('sale_line',key))
    add('InventoryMovement',dict(posting_key=ident('SALE-'+key),book_id=ident(book),quantity_delta=-qty,
        occurred_at_utc=f'2026-10-{21+sale:02}T02:00:00'))
returns=[('RETURN1-L1',1,'SALE1-L1',1),('RETURN2-L1',2,'SALE1-L1',1)]
for n in [1,2]:add('BookReturn',dict(return_id=ident(f'RETURN-{n}'),original_order_id=ident('SALE-1'),returned_at_utc=f'2026-10-{24+n:02}T03:00:00'))
for key,ret,sale_key,qty in returns:
    book=sale_book[sale_key]
    add('BookReturnLine',dict(return_id=ident(f'RETURN-{ret}'),original_order_line_id=ref('sale_line',sale_key),returned_quantity=qty,
        refund_amount=prices[book]*qty),('return_line',key))
    add('InventoryMovement',dict(posting_key=ident('RETURN-'+key),book_id=ident(book),quantity_delta=qty,occurred_at_utc=f'2026-10-{24+ret:02}T03:00:00'))

count_keys=['HISTORY','READY','NOCHANGE']
for key in count_keys:
    add('InventoryCount',dict(count_id=ident('COUNT-'+key),counted_at_utc='2026-10-27T04:00:00',recorded_by_employee_id=ident('EMP-INVENTORY')))
observations=[('H-B01-1','HISTORY','B01',42),('H-B01-2','HISTORY','B01',43),
 ('H-B02-1','HISTORY','B02',28),('H-B02-2','HISTORY','B02',27),('H-B03-1','HISTORY','B03',20),('H-B03-2','HISTORY','B03',18),
 ('R-B04-1','READY','B04',11),('R-B04-2','READY','B04',12),('N-B07-1','NOCHANGE','B07',24)]
for i,(key,count,book,qty) in enumerate(observations):
    add('InventoryCountObservation',dict(count_id=ident('COUNT-'+count),book_id=ident(book),counted_quantity=qty,
        observed_at_utc=f'2026-10-27T04:{i:02}:00'),('observation',key))
for count,book,obs,baseline in [('HISTORY','B01','H-B01-2',44),('HISTORY','B02','H-B02-2',27),
                              ('HISTORY','B03','H-B03-2',19),('NOCHANGE','B07','N-B07-1',24)]:
    add('InventoryCountSelection',dict(count_id=ident('COUNT-'+count),book_id=ident(book),selected_observation_id=ref('observation',obs),recorded_quantity=baseline))
add('InventoryAdjustment',dict(adjustment_id=ident('ADJUSTMENT-COUNT-HISTORY'),adjustment_date='2026-10-27',reason='[DEMO] Selected recount results; only Adjustment posts the difference.'))
add('CountAdjustment',dict(count_id=ident('COUNT-HISTORY'),adjustment_id=ident('ADJUSTMENT-COUNT-HISTORY')))
for book in ['B01','B03']:
    add('AdjustmentBook',dict(adjustment_id=ident('ADJUSTMENT-COUNT-HISTORY'),book_id=ident(book),quantity_change=-1))
    add('InventoryMovement',dict(posting_key=ident('ADJUSTMENT-COUNT-HISTORY-'+book),book_id=ident(book),quantity_delta=-1,occurred_at_utc='2026-10-27T04:10:00'))
add('InventoryAudit',dict(audit_id=ident('AUDIT-1'),audit_date='2026-10-27',status='APPROVED'))
add('InventoryAuditLine',dict(audit_id=ident('AUDIT-1'),book_id=ident('B01'),counted_quantity=40),('audit_line','1'))

for account,path,allowed in [('ADMIN','/admin/accounts',True),('MANAGER','/catalog/prices',True),('INVENTORY','/inventory/counts',True),
                            ('SALES-A','/sales/new',True),('SALES-B','/returns/new',False),('DISABLED','/sales/new',False)]:
    add('UserAccessLog',dict(account_id=ident('ACC-'+account),occurred_at_utc='2026-10-27T05:00:00',
        request_path='/__demo/swd392/20261027'+path,was_authorized=allowed,client_ip='127.0.0.1'),('access_log',account))
for service,operation in [('VnPay','qr-status'),('GoogleEmail','prepare-po-email'),('ViettelPost','shipment-status')]:
    add('ExternalServiceLog',dict(occurred_at_utc='2026-10-27T05:00:00',service_name='DEMO/SWD392/'+service,
        operation_name='fixture-only:no-network:'+operation,succeeded=True),('service_log',service))

model=json.loads((ROOT.parent/'schema-manifest.json').read_text(encoding='utf-8'))
tables={t['name']:t for t in model['tables']}
for row in rows:
    allowed={c['name'] for c in tables[row['table']]['columns']}
    assert set(row['data'])<=allowed,row
    if row['identity']:
        assert sum(c['identity'] for c in tables[row['table']]['columns'])==1
serializable=lambda o:o.json() if isinstance(o,Ref) else str(o)
payload=json.dumps(rows,ensure_ascii=False,sort_keys=True,default=serializable)
digest=hashlib.sha256(payload.encode()).hexdigest()
counts=Counter(r['table'] for r in rows);counts['BookStock']=len(books)
expected_stock={ident(book):final for book,_,_,_,final in books}
assert sum(expected_stock.values())==183
manifest=dict(dataset='SWD392 demo 2026-10-27 v1',prefix=PREFIX,target_database=DATABASE,marker=MARKER,
    payload_sha256=digest,expected_counts=dict(sorted(counts.items())),expected_stock=expected_stock,
    expected_total_units=183,expected_book_count=12,expected_catalog_value=27555000,
    expected_sale_total=1445000,expected_refund_total=260000,
    fixture_limits='Fictional snapshots, not approved business rules. No real payment/email/shipping, passwords or application authorization.',
    rows=rows)
(ROOT/'demo-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2,default=serializable)+'\n',encoding='utf-8')
sql=[f'-- Book Sales Management System — FICTIONAL DEMO DATA ONLY.\n-- Dataset v1; no business-policy approval is implied.\nUSE [{DATABASE}];\nGO',
     "SET NOCOUNT ON;\nSET XACT_ABORT ON;\nBEGIN TRY\nBEGIN TRANSACTION;",
     "DECLARE @lock_result int; EXEC @lock_result=sys.sp_getapplock @Resource=N'SWD392:demo:20261027:v1',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;\nIF @lock_result<0 THROW 51200,'Could not acquire demo seed lock.',1;",
     f"IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'{MARKER}')\nBEGIN\n IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'{MARKER}' AND CONVERT(nvarchar(4000),value)=N'{digest}') THROW 51201,'Seed marker differs. No existing demo data is overwritten.',1;\n COMMIT TRANSACTION;\n SELECT N'ALREADY_SEEDED' AS seed_status;\n RETURN;\nEND;",
     "DECLARE @FixtureIds TABLE(entity nvarchar(50) NOT NULL,fixture_key nvarchar(100) NOT NULL,identity_value bigint NOT NULL,PRIMARY KEY(entity,fixture_key));"]
guard_columns={t['name']:[c['name'] for c in t['columns'] if c['name'] in set(t['pk']) and c['sql_type'].startswith('nvarchar')] for t in model['tables']}
for table,columns in guard_columns.items():
    if columns:
        predicate=' OR '.join(f"{q(c)} LIKE N'{PREFIX}%'" for c in columns)
        sql.append(f"IF EXISTS(SELECT 1 FROM dbo.{q(table)} WHERE {predicate}) THROW 51202,'Demo namespace already contains unmarked records; refusing to overwrite.',1;")
usernames=[row['data']['username'] for row in rows if row['table']=='Account']
sql.append('IF EXISTS(SELECT 1 FROM dbo.Account WHERE username IN ('+','.join(map(literal,usernames))+")) THROW 51203,'Demo username already exists; refusing to overwrite.',1;")
sql.append(f"IF EXISTS(SELECT 1 FROM dbo.InventoryMovement WHERE posting_key LIKE N'{PREFIX}%') OR EXISTS(SELECT 1 FROM dbo.Shipment WHERE tracking_number LIKE N'{PREFIX}%') OR EXISTS(SELECT 1 FROM dbo.UserAccessLog WHERE request_path LIKE N'/__demo/swd392/20261027%') OR EXISTS(SELECT 1 FROM dbo.ExternalServiceLog WHERE service_name LIKE N'DEMO/SWD392/%') THROW 51202,'Demo namespace already contains unmarked records.',1;")
for row in rows:
    columns=list(row['data']); out=''
    if row['identity']:
        identity=next(c['name'] for c in tables[row['table']]['columns'] if c['identity'])
        kind,key=row['identity']
        out=f' OUTPUT {literal(kind)},{literal(key)},inserted.{q(identity)} INTO @FixtureIds(entity,fixture_key,identity_value)'
    sql.append(f"INSERT dbo.{q(row['table'])} ({','.join(map(q,columns))}){out}\nVALUES ({','.join(literal(row['data'][c]) for c in columns)});")
sql.append(f"INSERT dbo.BookStock(book_id,current_quantity)\nSELECT b.book_id,COALESCE(SUM(m.quantity_delta),CONVERT(decimal(38,10),0))\nFROM dbo.Book b LEFT JOIN dbo.InventoryMovement m ON m.book_id=b.book_id\nWHERE b.book_id LIKE N'{PREFIX}%' GROUP BY b.book_id;")
sql.append('DECLARE @ExpectedStock TABLE(book_id nvarchar(200) PRIMARY KEY,quantity decimal(38,10));\nINSERT @ExpectedStock VALUES '+','.join('('+literal(b)+','+str(q)+')' for b,q in expected_stock.items())+';')
sql.append("IF EXISTS(SELECT 1 FROM @ExpectedStock e LEFT JOIN dbo.BookStock s ON s.book_id=e.book_id WHERE s.book_id IS NULL OR s.current_quantity<>e.quantity) THROW 51204,'Demo final stock differs from the independently specified baseline.',1;")
sql.append(f"IF EXISTS(SELECT 1 FROM dbo.vStockReconciliation WHERE book_id LIKE N'{PREFIX}%' AND difference<>0) THROW 51205,'Demo stock does not reconcile.',1;")
sql += [f"EXEC sys.sp_addextendedproperty @name=N'{MARKER}',@value=N'{digest}';",
        "COMMIT TRANSACTION;\nSELECT N'SEEDED' AS seed_status;\nSELECT entity,fixture_key,identity_value FROM @FixtureIds ORDER BY entity,fixture_key;\nEND TRY\nBEGIN CATCH\n IF XACT_STATE()<>0 ROLLBACK TRANSACTION;\n THROW;\nEND CATCH;\nGO"]
(ROOT/'seed_demo.sql').write_text('\n\n'.join(sql)+'\n',encoding='utf-8')
scopes={
 'CustomerContact':"customer_id LIKE N'"+PREFIX+"%'",'ProviderContact':"provider_id LIKE N'"+PREFIX+"%'",
 'SalesOrderLine':"order_id LIKE N'"+PREFIX+"%'",'BookReturnLine':"return_id LIKE N'"+PREFIX+"%'",
 'GoodsReceiptLine':"receipt_id LIKE N'"+PREFIX+"%'",'PurchaseOrderLine':"purchase_order_id LIKE N'"+PREFIX+"%'",'Shipment':"tracking_number LIKE N'"+PREFIX+"%'",
 'InventoryCountObservation':"count_id LIKE N'"+PREFIX+"%'",'InventoryAuditLine':"audit_id LIKE N'"+PREFIX+"%'",
 'InventoryMovement':"posting_key LIKE N'"+PREFIX+"%'",
 'UserAccessLog':"request_path LIKE N'/__demo/swd392/20261027%'",'ExternalServiceLog':"service_name LIKE N'DEMO/SWD392/%'"}
verify=[f'-- Read-only checks for the original demo baseline. Edits during a demo may intentionally change these results.\nUSE [{DATABASE}];\nGO\nSET NOCOUNT ON;']
for table,count in sorted(counts.items()):
    scope=scopes.get(table)
    if scope is None:
        scope=' OR '.join(f"{q(c)} LIKE N'{PREFIX}%'" for c in guard_columns[table])
    assert scope,table
    verify.append(f"IF (SELECT COUNT_BIG(*) FROM dbo.{q(table)} WHERE {scope})<>{count} THROW 51210,'Demo baseline row count differs: {table}',1;")
verify.append('DECLARE @ExpectedStock TABLE(book_id nvarchar(200) PRIMARY KEY,quantity decimal(38,10));\nINSERT @ExpectedStock VALUES '+','.join('('+literal(b)+','+str(q)+')' for b,q in expected_stock.items())+';')
verify += [
 "IF EXISTS(SELECT 1 FROM @ExpectedStock e LEFT JOIN dbo.BookStock s ON s.book_id=e.book_id WHERE s.book_id IS NULL OR s.current_quantity<>e.quantity) THROW 51211,'Stock differs from demo baseline.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.vStockReconciliation WHERE book_id LIKE N'{PREFIX}%' AND difference<>0) THROW 51212,'Demo stock/ledger mismatch.',1;",
 f"IF (SELECT SUM(current_quantity) FROM dbo.BookStock WHERE book_id LIKE N'{PREFIX}%')<>183 THROW 51213,'Expected 183 demo units.',1;",
 f"IF (SELECT SUM(b.current_price*s.current_quantity) FROM dbo.Book b JOIN dbo.BookStock s ON s.book_id=b.book_id WHERE b.book_id LIKE N'{PREFIX}%')<>27555000 THROW 51214,'Catalog display value differs from baseline.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.SalesOrder o JOIN dbo.SalesOrderLine l ON l.order_id=o.order_id WHERE o.order_id LIKE N'{PREFIX}%' GROUP BY o.order_id,o.total_order_amount HAVING SUM(l.quantity*l.unit_price)<>o.total_order_amount) THROW 51215,'Demo order totals do not match their undiscounted lines.',1;",
 f"IF (SELECT SUM(total_order_amount) FROM dbo.SalesOrder WHERE order_id LIKE N'{PREFIX}%')<>1445000 THROW 51216,'Demo sale total differs.',1;",
 f"IF (SELECT SUM(refund_amount) FROM dbo.BookReturnLine WHERE return_id LIKE N'{PREFIX}%')<>260000 THROW 51217,'Demo refund total differs.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.BookReturnLine r JOIN dbo.BookReturn h ON h.return_id=r.return_id JOIN dbo.SalesOrderLine l ON l.order_line_id=r.original_order_line_id WHERE r.return_id LIKE N'{PREFIX}%' AND h.original_order_id<>l.order_id) THROW 51218,'Return references another order.',1;",
 f"IF EXISTS(SELECT r.original_order_line_id FROM dbo.BookReturnLine r JOIN dbo.SalesOrderLine l ON l.order_line_id=r.original_order_line_id WHERE r.return_id LIKE N'{PREFIX}%' GROUP BY r.original_order_line_id,l.quantity HAVING SUM(r.returned_quantity)>l.quantity) THROW 51219,'Return quantity exceeds purchased quantity.',1;",
 f"IF (SELECT COUNT(DISTINCT purchase_order_id) FROM dbo.vGoodsReceiptSources WHERE receipt_id=N'{ident('GR-1')}')<>2 THROW 51220,'Multi-PO receipt example missing.',1;",
 f"IF (SELECT COUNT(DISTINCT receipt_id) FROM dbo.vGoodsReceiptSources WHERE purchase_order_id=N'{ident('PO-1')}')<>2 THROW 51221,'Partial receiving example missing.',1;",
 f"IF (SELECT COUNT(*) FROM dbo.SalesOrderLine WHERE order_id=N'{ident('SALE-1')}' AND book_id=N'{ident('B01')}')<>2 THROW 51222,'Repeated Book sales-line example missing.',1;",
 f"IF (SELECT COUNT(*) FROM dbo.InventoryCountObservation WHERE count_id=N'{ident('COUNT-HISTORY')}' AND book_id=N'{ident('B01')}')<>2 THROW 51223,'Recount observations missing.',1;",
 f"IF (SELECT counted_quantity FROM dbo.vSelectedInventoryCount WHERE count_id=N'{ident('COUNT-HISTORY')}' AND book_id=N'{ident('B01')}')<>43 THROW 51224,'Selected recount must be 43, not 42+43.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.InventoryCountSelection WHERE count_id=N'{ident('COUNT-READY')}') THROW 51225,'Ready Count must have observations but no selected result yet.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.CountAdjustment WHERE count_id=N'{ident('COUNT-NOCHANGE')}') THROW 51226,'No-difference Count must not have an Adjustment.',1;",
 f"IF EXISTS(SELECT 1 FROM dbo.EmployeePermission WHERE employee_id=N'{ident('EMP-SALES-B')}' AND permission_id=N'{ident('UC21')}') THROW 51227,'Sales B is the missing-return-grant example.',1;",
 f"IF (SELECT COUNT(*) FROM dbo.EmployeePermission WHERE employee_id=N'{ident('EMP-ADMIN')}')<>21 THROW 51228,'Admin fixture must contain all demo grants.',1;",
 "IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51229,'A foreign key is disabled/untrusted.',1;",
 "SELECT N'BASELINE_VERIFIED' AS demo_status,223 AS fixture_rows,12 AS books,183 AS stock_units,1445000 AS gross_order_total,260000 AS recorded_refunds;\nGO"]
(ROOT/'verify_demo.sql').write_text('\n'.join(verify)+'\n',encoding='utf-8')
print(json.dumps(dict(counts=dict(sorted(counts.items())),total_rows=sum(counts.values()),units=183,payload_sha256=digest)))
