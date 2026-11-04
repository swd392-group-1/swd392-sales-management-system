"""Build the proposed review schema and reproducible, conditional FD evidence.

No server connection and no database mutation. Python 3 standard library only.
"""
from pathlib import Path
from itertools import combinations
import csv
import hashlib
import re
import json

ROOT = Path(__file__).resolve().parent
DATABASE = 'SWD392_BookSales_3NF_REVIEW_20261027'
TABLES = []

def col(name, sql_type='nvarchar(200)', nullable=False, identity=False):
    return dict(name=name, sql_type=sql_type, nullable=nullable, identity=identity)

def table(name, columns, pk, *, unique=(), foreign=(), checks=(), note='', source='PROPOSAL — not yet approved', extra_fds=()):
    TABLES.append(dict(name=name, columns=columns, pk=list(pk), unique=[list(k) for k in unique],
                       foreign=[dict(columns=list(a), table=b, references=list(c)) for a,b,c in foreign],
                       checks=list(checks), note=note, source=source,
                       extra_fds=[dict(lhs=list(a), rhs=list(b)) for a,b in extra_fds]))

ID = 'nvarchar(200)'
QTY = 'decimal(38,10)'
MONEY = 'decimal(19,4)'
TEXT = 'nvarchar(max)'
def fk(a, t, b):
    return ([a],t,[b])

table('Employee', [col('employee_id'), col('name',TEXT,True), col('role_code','nvarchar(32)')], ['employee_id'],
      checks=["[role_code] IN (N'SYSTEM_ADMIN',N'STORE_MANAGER',N'INVENTORY_STAFF',N'SALES_STAFF')"],
      note='One primary role confirmed. Placement on Employee is the normalization proposal; reconcile the PO note about EmployeePermission.')
table('Account', [col('account_id'),col('employee_id'),col('username','nvarchar(200)'),col('status',TEXT,True),col('password_hash','nvarchar(512)',True)], ['account_id'],
      unique=[['employee_id'],['username']], foreign=[fk('employee_id','Employee','employee_id')],
      note='One personal account per employee. FK+UNIQUE ensures at-most-one, not total Employee participation. password_hash stores only an application-generated password hash; NULL means credentials are not provisioned and Login must reject the account. No plaintext/default password. Login implementation remains with the module owner.')
table('Permission',[col('permission_id'),col('permission_name',TEXT,True)],['permission_id'],
      note='Permission ID is the proposed stable capability identifier. No seed grant matrix is invented.')
table('EmployeePermission',[col('employee_id'),col('permission_id')],['employee_id','permission_id'],
      foreign=[fk('employee_id','Employee','employee_id'),fk('permission_id','Permission','permission_id')],
      note='Only individual grants; primary role and names are not repeated here.')
table('UserAccessLog',[col('access_log_id','bigint',identity=True),col('account_id',nullable=True),
      col('occurred_at_utc','datetime2(7)'),col('request_path','nvarchar(2048)'),col('was_authorized','bit'),col('client_ip','nvarchar(45)')],['access_log_id'],
      foreign=[fk('account_id','Account','account_id')],
      note='PO requires page/denied-access/IP logs. Identity/time columns are proposed; unauthenticated requests may have no account. Retention/redaction open.')
table('ExternalServiceLog',[col('service_log_id','bigint',identity=True),col('occurred_at_utc','datetime2(7)'),
      col('service_name','nvarchar(100)'),col('operation_name','nvarchar(200)'),col('succeeded','bit')],['service_log_id'],
      note='PO requires external-service usage logs; minimal fields are proposed. No credentials or message payloads.')
table('Customer',[col('customer_id'),col('name',TEXT,True)],['customer_id'])
table('CustomerContact',[col('contact_id','bigint',identity=True),col('customer_id'),col('contact_kind','nvarchar(50)'),col('contact_value','nvarchar(1000)')],['contact_id'],
      foreign=[fk('customer_id','Customer','customer_id')],
      note='Proposed atomic contact entries replace ambiguous contact_information. One value per row; type vocabulary/validation remain open. No copied Customer name.')
table('Provider',[col('provider_id'),col('name',TEXT,True)],['provider_id'])
table('ProviderContact',[col('contact_id','bigint',identity=True),col('provider_id'),col('contact_kind','nvarchar(50)'),col('contact_value','nvarchar(1000)')],['contact_id'],
      foreign=[fk('provider_id','Provider','provider_id')],
      note='Proposed atomic contact entries. Original export has zero business rows at the inspected time; no unreviewed string-splitting migration.')
table('Book',[col('book_id'),col('title',TEXT,True),col('current_price',MONEY,True)],['book_id'],
      note='Source catalog attributes retained. Transaction price capture timing and price-change policies remain open.')
table('Promotion',[col('promotion_id'),col('name',TEXT,True),col('discount_kind','nvarchar(50)',True),
      col('discount_value',MONEY,True),col('start_date','date',True),col('end_date','date',True)],['promotion_id'],
      note='PROPOSED typed discount value plus an explicit unit/kind replaces opaque source text. No allowed kind list, percentage bounds or stacking formula is assumed.')
table('PromotionBook',[col('promotion_id'),col('book_id')],['promotion_id','book_id'],
      foreign=[fk('promotion_id','Promotion','promotion_id'),fk('book_id','Book','book_id')],
      note='Source pair relationship retained for review; exact cardinality/overlap rules remain open.')
table('SalesOrder',[col('order_id'),col('order_date','date',True),col('total_order_amount',MONEY,True),col('customer_id',nullable=True)],['order_id'],
      foreign=[fk('customer_id','Customer','customer_id')],
      note='Source total retained as recorded transaction total; discount allocation/cost/profit rules are not invented.')
table('SalesOrderLine',[col('order_line_id','bigint',identity=True),col('order_id'),col('book_id'),col('quantity',QTY),col('unit_price',MONEY,True)],['order_line_id'],
      foreign=[fk('order_id','SalesOrder','order_id'),fk('book_id','Book','book_id')], checks=['[quantity] > 0'],
      note='Replaces OrderBook pair PK. Repeated Book lines supported; line identity is a technical proposal. Unit price is source transaction data, not a new snapshot-timing rule.')
table('Payment',[col('payment_id'),col('payment_method',TEXT,True),col('payment_status',TEXT,True)],['payment_id'],
      note='Source fields retained. Settlement amount, attempts, allocation, QR callbacks and idempotency rules remain open.')
table('OrderPayment',[col('order_id'),col('payment_id')],['order_id','payment_id'],
      foreign=[fk('order_id','SalesOrder','order_id'),fk('payment_id','Payment','payment_id')],
      note='PROVISIONAL source link, not confirmation that payments may cover multiple orders. Revisit when payment cardinality is decided.')
table('BookReturn',[col('return_id'),col('original_order_id'),col('returned_at_utc','datetime2(7)')],['return_id'],
      foreign=[fk('original_order_id','SalesOrder','order_id')],
      note='Confirmed: one original order per return, multiple returns per order. Cash-only refund is a policy, not a repeated constant method column.')
table('BookReturnLine',[col('return_line_id','bigint',identity=True),col('return_id'),col('original_order_line_id','bigint'),
      col('returned_quantity',QTY),col('refund_amount',MONEY)],['return_line_id'],
      foreign=[fk('return_id','BookReturn','return_id'),fk('original_order_line_id','SalesOrderLine','order_line_id')],
      checks=['[returned_quantity] > 0','[refund_amount] >= 0'],
      note='Do not repeat original_order_id/book_id/unit_price: the referenced sales line determines them. Trigger enforces same original order; cumulative quantity uses transactional validation. Eligibility/refund allocation remain open.')
table('PurchaseOrder',[col('purchase_order_id'),col('order_date','date',True),col('status',TEXT,True)],['purchase_order_id'])
table('PurchaseOrderLine',[col('purchase_order_line_id','bigint',identity=True),col('purchase_order_id'),col('book_id'),col('ordered_quantity',QTY)],['purchase_order_line_id'],
      foreign=[fk('purchase_order_id','PurchaseOrder','purchase_order_id'),fk('book_id','Book','book_id')],checks=['[ordered_quantity] > 0'],
      note='Replaces PurchaseOrderBook pair key; repeated Book lines allowed. No inferred purchase cost or line price.')
table('PurchaseOrderProvider',[col('purchase_order_id'),col('provider_id')],['purchase_order_id','provider_id'],
      foreign=[fk('purchase_order_id','PurchaseOrder','purchase_order_id'),fk('provider_id','Provider','provider_id')],
      note='PROVISIONAL source link; one-versus-multiple providers per PO remains open. Not a newly confirmed M:N business rule.')
table('GoodsReceipt',[col('receipt_id'),col('receipt_date','date',True)],['receipt_id'],
      note='Confirmed multi-PO receipt: no purchase_order_id on header; PO membership derives from receipt lines.')
table('GoodsReceiptLine',[col('receipt_line_id','bigint',identity=True),col('receipt_id'),col('purchase_order_line_id','bigint'),col('received_quantity',QTY)],['receipt_line_id'],
      foreign=[fk('receipt_id','GoodsReceipt','receipt_id'),fk('purchase_order_line_id','PurchaseOrderLine','purchase_order_line_id')],checks=['[received_quantity] > 0'],
      note='References exact PO line. Do not repeat PO ID or Book ID: they depend on purchase_order_line_id. Multiple POs per receipt and multiple receipts per PO supported.')
table('Shipment',[col('shipment_id','bigint',identity=True),col('tracking_number'),col('shipment_status',TEXT,True)],['shipment_id'],
      note='Technical shipment ID avoids assuming tracking numbers are globally unique. Carrier namespace/reuse rules remain open; no tracking-number UNIQUE is asserted.')
table('ProviderShipment',[col('provider_id'),col('shipment_id','bigint')],['provider_id','shipment_id'],
      foreign=[fk('provider_id','Provider','provider_id'),fk('shipment_id','Shipment','shipment_id')],note='PROVISIONAL source pair relation.')
table('PurchaseOrderShipment',[col('purchase_order_id'),col('shipment_id','bigint')],['purchase_order_id','shipment_id'],
      foreign=[fk('purchase_order_id','PurchaseOrder','purchase_order_id'),fk('shipment_id','Shipment','shipment_id')],note='PROVISIONAL source pair relation.')
table('PurchaseOrderApproval',[col('employee_id'),col('purchase_order_id')],['employee_id','purchase_order_id'],
      foreign=[fk('employee_id','Employee','employee_id'),fk('purchase_order_id','PurchaseOrder','purchase_order_id')],
      note='Source pair retained. Role authorization and repeated approval/history semantics are not implemented by this link.')
table('InventoryAudit',[col('audit_id'),col('audit_date','date',True),col('status',TEXT,True)],['audit_id'],
      note='Audit/approval does not update stock or automatically create Adjustment; no trigger does so.')
table('InventoryAuditLine',[col('audit_line_id','bigint',identity=True),col('audit_id'),col('book_id'),col('counted_quantity',QTY)],['audit_line_id'],
      foreign=[fk('audit_id','InventoryAudit','audit_id'),fk('book_id','Book','book_id')],checks=['[counted_quantity] >= 0'],
      note='Proposed observation identity; Audit observation/recount grain remains under review, distinct from independent Count.')
table('InventoryCount',[col('count_id'),col('counted_at_utc','datetime2(7)'),col('recorded_by_employee_id')],['count_id'],
      foreign=[fk('recorded_by_employee_id','Employee','employee_id')],note='Independent Count, not an Audit or its approval.')
table('InventoryCountObservation',[col('observation_id','bigint',identity=True),col('count_id'),col('book_id'),
      col('counted_quantity',QTY),col('observed_at_utc','datetime2(7)')],['observation_id'],
      unique=[['observation_id','count_id','book_id']],
      foreign=[fk('count_id','InventoryCount','count_id'),fk('book_id','Book','book_id')],checks=['[counted_quantity] >= 0'],
      note='Confirmed repeated Book rows mean recounts. Each observation is retained, never summed as independent stock. Extra triple UNIQUE exists only as a composite FK target, not a minimal candidate key.')
table('InventoryCountSelection',[col('count_id'),col('book_id'),col('selected_observation_id','bigint'),col('recorded_quantity',QTY)],['count_id','book_id'],
      unique=[['selected_observation_id']],
      foreign=[(['selected_observation_id','count_id','book_id'],'InventoryCountObservation',['observation_id','count_id','book_id'])],
      note='One chosen final observation per Count/Book; it need not be MAX(time). Composite FK proves selected observation matches that Count and Book. recorded_quantity is the historical comparison baseline, not current BookStock.')
table('InventoryAdjustment',[col('adjustment_id'),col('adjustment_date','date',True),col('reason',TEXT,True)],['adjustment_id'],
      note='quantity_change moved off header to per-Book line. Manual adjustment semantics remain open.')
table('AdjustmentBook',[col('adjustment_id'),col('book_id'),col('quantity_change',QTY)],['adjustment_id','book_id'],
      foreign=[fk('adjustment_id','InventoryAdjustment','adjustment_id'),fk('book_id','Book','book_id')],
      note='One delta per Book per Adjustment, proposed grain supported by one Adjustment containing differing Books of Count.')
table('CountAdjustment',[col('count_id'),col('adjustment_id')],['count_id'],unique=[['adjustment_id']],
      foreign=[fk('count_id','InventoryCount','count_id'),fk('adjustment_id','InventoryAdjustment','adjustment_id')],
      note='At most one resulting Adjustment per Count and one source Count per automatic Adjustment. No link is needed if Count has no differences.')
table('BookStock',[col('book_id'),col('current_quantity',QTY)],['book_id'],foreign=[fk('book_id','Book','book_id')],
      note='Confirmed stored current stock. No nonnegative-stock or opening balance is assumed. No automated posting workflow is created by this table.')
table('InventoryMovement',[col('movement_id','bigint',identity=True),col('posting_key','nvarchar(200)'),col('book_id'),
      col('quantity_delta',QTY),col('occurred_at_utc','datetime2(7)')],['movement_id'],unique=[['posting_key']],
      foreign=[fk('book_id','Book','book_id')],
      note='Proposed immutable movement history; opaque stable posting_key supports idempotency. It is not a polymorphic FK or a guarantee of source-document consistency. Posting+BookStock must be atomic in future application code.')

LOOKUP = {t['name']:t for t in TABLES}
for t in TABLES:
    names={c['name'] for c in t['columns']}
    assert set(t['pk'])<=names
    for u in t['unique']: assert set(u)<=names
    for f in t['foreign']:
        target=LOOKUP[f['table']]
        assert set(f['columns'])<=names
        assert f['references'] in [target['pk']]+target['unique'],f
        assert len(f['columns'])==len(f['references'])
        for a,b in zip(f['columns'],f['references']):
            assert next(c['sql_type'] for c in t['columns'] if c['name']==a)==next(c['sql_type'] for c in target['columns'] if c['name']==b)

def quote(n): return '['+n+']'
def cols(a): return ', '.join(map(quote,a))
def closure(seed, fds):
    result=set(seed)
    while True:
        before=set(result)
        for lhs,rhs in fds:
            if lhs<=result: result |= rhs
        if before==result: return result

def analyze(attrs, keys, extra_fds=()):
    attrs=set(attrs); keys=[set(k) for k in keys]
    fds=[(k,attrs-k) for k in keys]+[(set(a),set(b)) for a,b in extra_fds]
    candidates=[]
    for size in range(1,len(attrs)+1):
        for choice in combinations(sorted(attrs),size):
            s=set(choice)
            if any(k<=s for k in candidates): continue
            if closure(s,fds)==attrs: candidates.append(s)
    prime=set().union(*candidates)
    violations2=[];violations3=[];violations_bcnf=[]
    # Check every implied determinant, not only the listed PK and not just listed FD rows.
    ordered=sorted(attrs)
    for size in range(len(attrs)+1):
        for choice in combinations(ordered,size):
            determinant=set(choice); implied=closure(determinant,fds)-determinant
            superkey=closure(determinant,fds)==attrs
            for attribute in implied:
                if not superkey: violations_bcnf.append([list(choice),attribute])
                if not superkey and attribute not in prime: violations3.append([list(choice),attribute])
                if attribute not in prime and any(determinant<k for k in candidates): violations2.append([list(choice),attribute])
    return candidates,fds,violations2,violations3,violations_bcnf

proof=[]
for t in TABLES:
    attrs={c['name'] for c in t['columns']}
    keys=[set(k) for k in [t['pk']]+t['unique']]
    # Every declared key is NOT NULL in this candidate; no nullable alternate-key shortcuts.
    for key in keys: assert all(not c['nullable'] for c in t['columns'] if c['name'] in key)
    candidates,fds,violations2,violations3,violations_bcnf=analyze(attrs,keys,
        [(f['lhs'],f['rhs']) for f in t['extra_fds']])
    assert not violations2 and not violations3,t['name']
    proof.append(dict(table=t['name'],candidate_keys=[sorted(k) for k in candidates],
                      fds=[dict(lhs=sorted(a),rhs=sorted(b)) for a,b in fds if b],
                      conditional_2nf=True,conditional_3nf=True,conditional_bcnf=not violations_bcnf,
                      qualification='Under the explicitly declared FD set and atomic-domain interpretation only; not completeness/business approval.',
                      notes=t['note']))

# Counterexamples exercise the same checker, rather than assuming that adding a PK solves 3NF.
bad_grant=analyze(['employee','permission','role','permission_name'],[['employee','permission']],
                  [(['employee'],['role']),(['permission'],['permission_name'])])
assert bad_grant[2] and bad_grant[3]
bad_receipt=analyze(['line','receipt','po_line','po','book','quantity'],[['line']],
                    [(['po_line'],['po','book'])])
assert not bad_receipt[2] and bad_receipt[3]
valid_one_to_one=analyze(['account','employee','username','employee_name'],[['account'],['employee'],['username']],
                         [(['employee'],['employee_name'])])
assert not valid_one_to_one[2] and not valid_one_to_one[3]

def lossless_chase(attributes, fds, decomposition):
    """FD tableau chase; distinguished a symbols win unification."""
    attributes=sorted(attributes); parent={}
    def find(x):
        parent.setdefault(x,x)
        if parent[x]!=x: parent[x]=find(parent[x])
        return parent[x]
    def unite(a,b):
        a,b=find(a),find(b)
        if a==b: return False
        preferred=sorted([a,b],key=lambda x:(not x.startswith('a:'),x))[0]
        parent[b if preferred==a else a]=preferred
        return True
    rows=[{a:('a:'+a if a in part else f'b:{i}:{a}') for a in attributes} for i,part in enumerate(decomposition)]
    changed=True
    while changed:
        changed=False
        for lhs,rhs in fds:
            for i in range(len(rows)):
                for j in range(i+1,len(rows)):
                    if all(find(rows[i][a])==find(rows[j][a]) for a in lhs):
                        for a in rhs: changed=unite(rows[i][a],rows[j][a]) or changed
    return any(all(find(row[a])==find('a:'+a) for a in attributes) for row in rows)

def projected_fds(part, fds):
    result=[]; ordered=sorted(part)
    for size in range(len(ordered)+1):
        for choice in combinations(ordered,size):
            lhs=set(choice); rhs=(closure(lhs,fds)&set(part))-lhs
            if rhs: result.append((lhs,rhs))
    return result

decomposition_examples=[
    ('Employee and permission details',{'e','p','name','role','permission_name'},
     [({'e','p'},{'name','role','permission_name'}),({'e'},{'name','role'}),({'p'},{'permission_name'})],
     [{'e','name','role'},{'p','permission_name'},{'e','p'}]),
    ('Receipt line and original PO line',{'line','receipt','po_line','po','book','quantity'},
     [({'line'},{'receipt','po_line','po','book','quantity'}),({'po_line'},{'po','book'})],
     [{'line','receipt','po_line','quantity'},{'po_line','po','book'}]),
    ('Return header, return line and original sales line',{'rl','r','sl','o','book','date','quantity','refund'},
     [({'rl'},{'r','sl','o','book','date','quantity','refund'}),({'r'},{'o','date'}),({'sl'},{'o','book'})],
     [{'rl','r','sl','quantity','refund'},{'r','o','date'},{'sl','o','book'}]),
    ('Count header and recount observations',{'observation','count','book','quantity','time','count_date','employee'},
     [({'observation'},{'count','book','quantity','time','count_date','employee'}),({'count'},{'count_date','employee'})],
     [{'observation','count','book','quantity','time'},{'count','count_date','employee'}]),
    ('Per-Book Adjustment grain',{'adjustment','book','delta','date','reason'},
     [({'adjustment','book'},{'delta','date','reason'}),({'adjustment'},{'date','reason'})],
     [{'adjustment','date','reason'},{'adjustment','book','delta'}]),
]
decomposition_proof=[]
for name,attributes,fds,parts in decomposition_examples:
    projected=[fd for part in parts for fd in projected_fds(part,fds)]
    preserved=all(rhs<=closure(lhs,projected) for lhs,rhs in fds)
    lossless=lossless_chase(attributes,fds,parts)
    assert lossless and preserved,name
    decomposition_proof.append(dict(example=name,attributes=sorted(attributes),
        fds=[dict(lhs=sorted(a),rhs=sorted(b)) for a,b in fds],relations=[sorted(p) for p in parts],
        lossless_under_declared_fds=lossless,dependency_preserving_under_declared_fds=preserved))
assert not lossless_chase({'a','b','c'},[({'a'},{'b'})],[{'a','b'},{'b','c'}])
nonpreserved_parts=[{'a','b'},{'a','c'}]; nonpreserved_fds=[({'a'},{'b'}),({'b'},{'c'})]
assert lossless_chase({'a','b','c'},nonpreserved_fds,nonpreserved_parts)
assert 'c' not in closure({'b'},[fd for part in nonpreserved_parts for fd in projected_fds(part,nonpreserved_fds)])
(ROOT/'decomposition-proof.json').write_text(json.dumps(dict(examples=decomposition_proof,
    checker_counterexamples=['Partial Role dependency rejected','Transitive PO-line dependency rejected',
                            '1:1 FK candidate key correctly accepted','Lossy decomposition rejected','Lost dependency detected'],
    qualification='Logical examples only. Not a proof that old aggregated or opaque values can be migrated automatically.'),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

banner='-- PROPOSAL — not yet approved. Fresh review database only; not an in-place migration.\n'
create=banner+f"USE [master];\nGO\nIF DB_ID(N'{DATABASE}') IS NOT NULL\n    THROW 51000, 'Review database already exists. No overwrite or drop is performed.', 1;\nGO\nCREATE DATABASE [{DATABASE}];\nGO\n"
(ROOT/'01_create_database.sql').write_text(create,encoding='utf-8')
ddl=[banner,f'USE [{DATABASE}];\nGO', 'SET ANSI_NULLS ON;\nSET QUOTED_IDENTIFIER ON;\nSET XACT_ABORT ON;\nGO',
     "IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped=0) THROW 51001, 'Target is not empty.', 1;\nGO",'BEGIN TRANSACTION;']
for t in TABLES:
    lines=[]
    for c in t['columns']:
        lines.append('    '+quote(c['name'])+' '+c['sql_type']+(' IDENTITY(1,1)' if c['identity'] else '')+(' NULL' if c['nullable'] else ' NOT NULL'))
    lines.append(f"    CONSTRAINT [PK_{t['name']}] PRIMARY KEY ({cols(t['pk'])})")
    for i,u in enumerate(t['unique'],1): lines.append(f"    CONSTRAINT [UQ_{t['name']}_{i}] UNIQUE ({cols(u)})")
    for i,ch in enumerate(t['checks'],1): lines.append(f"    CONSTRAINT [CK_{t['name']}_{i}] CHECK ({ch})")
    ddl += ['-- '+(t['note'] or 'Source entity attributes retained for review.'),f"CREATE TABLE [dbo].[{t['name']}] (\n"+',\n'.join(lines)+'\n);']
for t in TABLES:
    for i,f in enumerate(t['foreign'],1):
        ddl.append(f"ALTER TABLE [dbo].[{t['name']}] WITH CHECK ADD CONSTRAINT [FK_{t['name']}_{i}] FOREIGN KEY ({cols(f['columns'])}) REFERENCES [dbo].[{f['table']}] ({cols(f['references'])});")
ddl+=['COMMIT TRANSACTION;\nGO']
(ROOT/'02_create_schema.sql').write_text('\n\n'.join(ddl)+'\n',encoding='utf-8')
manifest=dict(database=DATABASE,status='PROPOSAL — not yet approved; conditional 3NF candidate',tables=TABLES,
              expected=dict(tables=len(TABLES),columns=sum(len(t['columns']) for t in TABLES),primary_keys=len(TABLES),
                            unique_constraints=sum(len(t['unique']) for t in TABLES),foreign_keys=sum(len(t['foreign']) for t in TABLES),
                            checks=sum(len(t['checks']) for t in TABLES)))
(ROOT/'schema-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(ROOT/'fd-proof.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
mapping={
    'Account':(['Account'],'Add personal-owner FK, uniqueness and nullable password_hash for safely provisioning existing demo accounts; no plaintext credentials or default passwords.'),
    'AdjustmentBook':(['AdjustmentBook'],'Move per-Book quantity_change here; original header quantity cannot be allocated automatically.'),
    'Book':(['Book'],'Retain source catalog attributes.'),
    'Customer':(['Customer','CustomerContact'],'Proposed atomic contact rows; no automatic splitting of opaque source text.'),
    'Employee':(['Employee'],'Add confirmed primary-role value; placement remains a PO-review item.'),
    'EmployeeAccount':(['Account'],'Remove junction under proposed personal 1:1 ownership; no source data migrated.'),
    'EmployeePermission':(['EmployeePermission'],'Keep grant pair; never repeat primary role or names.'),
    'GoodsReceipt':(['GoodsReceipt','GoodsReceiptLine'],'Remove single-PO header FK/UNIQUE; derive PO set from exact referenced PO lines.'),
    'GoodsReceiptBook':(['GoodsReceiptLine'],'Introduce occurrence ID and exact PO-line FK; Book ID is derived.'),
    'InventoryAdjustment':(['InventoryAdjustment','AdjustmentBook'],'Header properties separated from per-Book differences.'),
    'InventoryAudit':(['InventoryAudit'],'Retain independent Audit data; no stock posting on approval.'),
    'InventoryAuditBook':(['InventoryAuditLine'],'Proposed observation key; Audit grain remains review scope.'),
    'OrderBook':(['SalesOrderLine'],'Occurrence identity supports repeated Books and original-line Return references.'),
    'OrderPayment':(['OrderPayment'],'Keep provisional link pending payment cardinality.'),
    'Payment':(['Payment'],'Retain source attributes; settlement model remains incomplete.'),
    'Permission':(['Permission'],'Retain ID/name without repeating name in grants.'),
    'Promotion':(['Promotion'],'Proposed typed value/kind; formula, units and legacy conversion remain open.'),
    'PromotionBook':(['PromotionBook'],'Keep provisional source link.'),
    'Provider':(['Provider','ProviderContact'],'Proposed atomic contact rows.'),
    'ProviderShipment':(['ProviderShipment'],'Use technical Shipment ID, not assumed globally unique tracking number.'),
    'PurchaseOrder':(['PurchaseOrder'],'Retain source header attributes.'),
    'PurchaseOrderApproval':(['PurchaseOrderApproval'],'Retain source approval association and Employee identity.'),
    'PurchaseOrderBook':(['PurchaseOrderLine'],'Occurrence ID supports repeated Books and receipt allocation.'),
    'PurchaseOrderProvider':(['PurchaseOrderProvider'],'Keep provisional source link; provider cardinality unresolved.'),
    'PurchaseOrderShipment':(['PurchaseOrderShipment'],'Use technical Shipment ID.'),
    'SalesOrder':(['SalesOrder'],'Retain original header and nullable customer relation.'),
    'Shipment':(['Shipment'],'Introduce technical ID; tracking-number namespace remains unresolved.'),
}
source_path=ROOT.parents[2]/'SWD392_BookStore.sql'
source_hash=None
if source_path.exists():
    raw=source_path.read_bytes()
    source_text=raw.decode('utf-16') if raw.startswith(b'\xff\xfe') else raw.decode('utf-8-sig')
    assert set(re.findall(r'CREATE TABLE \[dbo\]\.\[([^]]+)\]',source_text))==set(mapping)
    source_hash=hashlib.sha256(raw).hexdigest()
source_audit=dict(source_file='SWD392_BookStore.sql (workspace parent, not copied into repository)',
    source_sha256=source_hash or 'source not present on this machine; see reviewed generation history',
    mapping=[dict(source_table=name,candidate_tables=targets,change=note) for name,(targets,note) in sorted(mapping.items())],
    new_requirement_support=['UserAccessLog','ExternalServiceLog','BookReturn','BookReturnLine','InventoryCount',
                             'InventoryCountObservation','InventoryCountSelection','CountAdjustment','BookStock','InventoryMovement'],
    qualification='All 27 source tables mapped; not an automatic data migration or proof that unresolved source cardinalities are approved.')
(ROOT/'source-to-candidate.json').write_text(json.dumps(source_audit,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (ROOT/'normalization-matrix.csv').open('w',encoding='utf-8-sig',newline='') as out:
    writer=csv.writer(out); writer.writerow(['Relation','Candidate keys (under declared FDs)','FDs','2NF','3NF','Notes'])
    for p in proof:
        writer.writerow([p['table'],'; '.join('('+','.join(k)+')' for k in p['candidate_keys']),
                         '; '.join(','.join(f['lhs'])+' -> '+','.join(f['rhs']) for f in p['fds']),
                         'CONDITIONAL PASS','CONDITIONAL PASS',p['notes']])
checks=[banner,f'USE [{DATABASE}];\nGO','SET NOCOUNT ON;']
helper_filter="parent_object_id NOT IN (SELECT object_id FROM sys.tables WHERE name=N'sysdiagrams')"
catalog_conditions=[('tables',"sys.tables WHERE is_ms_shipped=0 AND name<>N'sysdiagrams'"),
                    ('primary_keys',"sys.key_constraints WHERE type='PK' AND "+helper_filter),
                    ('unique_constraints',"sys.key_constraints WHERE type='UQ' AND "+helper_filter),
                    ('foreign_keys','sys.foreign_keys WHERE '+helper_filter),('checks','sys.check_constraints WHERE '+helper_filter)]
for name,source in catalog_conditions:
    checks.append(f"IF (SELECT COUNT(*) FROM {source}) <> {manifest['expected'][name]} THROW 51002, 'Unexpected {name} count.', 1;")
checks += ["IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51003, 'Untrusted FK.', 1;",
           "IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THROW 51004, 'Untrusted CHECK.', 1;",
           "SELECT SCHEMA_NAME(schema_id) AS schema_name,name FROM sys.tables WHERE is_ms_shipped=0 AND name<>N'sysdiagrams' ORDER BY name;",
           "SELECT name,is_disabled,is_not_trusted FROM sys.foreign_keys ORDER BY name;"]
(ROOT/'04_verify_schema.sql').write_text('\n'.join(checks)+'\n',encoding='utf-8')
print(json.dumps(manifest['expected']))
print(f'FD closure checked all determinants for {len(proof)} relations: conditional 2NF/3NF PASS.')
