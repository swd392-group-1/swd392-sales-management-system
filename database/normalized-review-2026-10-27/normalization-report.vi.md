# Phân tích 1NF 2NF 3NF của database nhà sách

Ngày 27/10/2026. **PROPOSAL — not yet approved.** Mục tiêu là sửa cấu trúc dữ liệu theo các quy tắc hiện hành, ghi rõ grain/khóa/phụ thuộc hàm, và kiểm tra trên SQL Server một database review riêng. Kết quả toán học chỉ đúng dưới tập FD và miền giá trị đã khai báo; cần PO review tính đầy đủ của các quy tắc còn thiếu.

## Nguồn và phạm vi

Nguồn cấu trúc là `SWD392_BookStore.sql`, export 22/10/2026, SHA-256 `1563337b07618b6e12c94b9cb2f57b0f5b95ac7a8facd17a294ed9c888cd738a`. Nguồn nghiệp vụ là context chuẩn và các quyết định trực tiếp ngày 27/10: mỗi nhân viên một account/role chính; Admin quản trị toàn bộ quyền, Manager quản lý nghiệp vụ; phiếu nhận gộp nhiều PO; Count lưu các lần đếm lại rồi chọn kết quả; lưu tồn hiện tại và lịch sử. Return dùng các quy tắc đã xác nhận trong context, gồm một đơn gốc, tham chiếu dòng bán, trả nhiều lần, hoàn tiền mặt và nhập lại sách được nhận trả.

Ánh xạ đầy đủ 27 bảng nguồn nằm trong `source-to-candidate.json`. Bản mới có 38 bảng: bỏ EmployeeAccount, thêm 12 bảng để tách contact và hỗ trợ Count, Return, tồn kho và log. **Số bảng tăng không phải bằng chứng đạt 3NF.** Các bảng mới phục vụ phạm vi nghiệp vụ được yêu cầu; không được gọi tất cả là kết quả phân rã thuần túy của file cũ.

Database cũ được đọc catalog/row count ngày 27/10: không có dòng nghiệp vụ. Bản review tạo mới, không chuyển dữ liệu. Khi có dữ liệu thật, vẫn phải giải quyết ánh xạ và kiểm tra trước khi migrate; việc schema mới chạy được không chứng minh chuyển dữ liệu cũ là lossless.

## Grain và khóa

| Nhóm | Grain quan trọng | Khóa ứng viên đề xuất |
|---|---|---|
| Employee | Một nhân viên | employee_id |
| Account | Một account cá nhân của một Employee | account_id; employee_id; username nếu chốt quy tắc username duy nhất |
| Permission | Một quyền được định danh | permission_id |
| EmployeePermission | Một grant Employee–Permission | (employee_id, permission_id) |
| Contact | Một giá trị liên hệ của một chủ thể | contact_id; không khẳng định tên hay contact_value là duy nhất |
| SalesOrderLine | Một lần xuất hiện của Book trong đơn | order_line_id; không dùng (order_id, book_id) |
| PurchaseOrderLine | Một lần xuất hiện của Book trong PO | purchase_order_line_id |
| GoodsReceiptLine | Một dòng nhận gắn một dòng PO cụ thể | receipt_line_id |
| CountObservation | Một lần đếm một Book trong một Count | observation_id |
| CountSelection | Một kết quả được chọn cho Count/Book | (count_id, book_id); selected_observation_id |
| AdjustmentBook | Chênh lệch của một Book trong Adjustment | (adjustment_id, book_id) |
| CountAdjustment | Một liên kết Count–Adjustment tự động | count_id; adjustment_id |
| BookReturn | Một lần trả thuộc một đơn bán gốc | return_id |
| BookReturnLine | Một dòng được nhận trả theo dòng bán gốc | return_line_id |
| BookStock | Tồn hiện tại của một Book | book_id |
| InventoryMovement | Một biến động kho đã ghi với khóa posting ổn định | movement_id; posting_key |

Khóa thay thế IDENTITY chỉ định danh occurrence; nó không tự loại bỏ phụ thuộc không khóa. Các ID business nvarchar(200), username 200, mã role 32, tiền decimal(19,4), quantity decimal(38,10) là lựa chọn kỹ thuật của bản review, không xác nhận độ dài/đơn vị/làm tròn nghiệp vụ. Không giả định quantity luôn nguyên hoặc tồn luôn không âm.

## Bước 1 kiểm tra 1NF

- Mỗi dòng có danh tính hoặc cặp khóa xác định đúng grain; cho phép Book lặp bằng các dòng riêng.
- Mỗi permission grant là một dòng; không lưu danh sách permission ID bằng CSV/JSON trong Employee hoặc Account.
- Contact tách thành loại và **một** giá trị trên mỗi dòng. Đây là miền đề xuất; không tự tách một chuỗi liên hệ cũ bằng dấu phẩy vì địa chỉ cũng có thể chứa dấu phẩy. Định nghĩa loại/validation còn cần review.
- Promotion có discount_kind và discount_value dạng số để phân biệt đơn vị/ý nghĩa; chưa áp đặt loại giảm giá, giới hạn phần trăm hoặc cách cộng dồn. Đây là thay đổi kiểu/miền đề xuất, không phải phép chuyển đổi an toàn mọi chuỗi discount_value cũ.
- Nullable giữ ý nghĩa tùy chọn/chưa có dữ liệu của một số thuộc tính nguồn. Không dùng NULL như bằng chứng chứng minh FD; mọi khóa ứng viên được kiểm tra trong mô hình đề xuất đều không NULL.

## Bước 2 loại phụ thuộc bộ phận để đạt 2NF

Ví dụ quan hệ chưa phân rã:

```text
GrantDetails(employee_id, permission_id, employee_name, role_code, permission_name)
Khóa: (employee_id, permission_id)
employee_id -> employee_name, role_code
permission_id -> permission_name
```

Các thuộc tính không khóa phụ thuộc vào một phần khóa ghép. Tách thành Employee, Permission, EmployeePermission. Lần lượt JOIN qua employee_id và permission_id là lossless dưới các FD trên vì mỗi giao chứa khóa của quan hệ chi tiết được tách. FD của tên/role được giữ ở đúng quan hệ.

Trong SQL gốc, EmployeePermission vốn chỉ có hai cột khóa; nó không mắc lỗi này. Lỗi phát sinh nếu thêm Role chính vào từng dòng như câu ghi chú PO. Bản review đặt Role trên Employee để thể hiện `employee_id -> role_code`, nhưng vị trí này phải được đối chiếu với PO trước khi coi là thiết kế được nghiệm thu.

Adjustment cũng cần đúng grain: ngày/lý do phụ thuộc adjustment_id; quantity_change phụ thuộc cặp (adjustment_id, book_id). Vì một Adjustment có nhiều Book, không giữ một lượng thay đổi chung trên header. Giá trị header của database cũ không thể tự phân bổ thành nhiều chênh lệch từng Book khi chưa có bằng chứng nguồn.

## Bước 3 loại phụ thuộc bắc cầu để đạt 3NF

### Nhận hàng từ nhiều PO

Nếu giữ cùng lúc các cột sau trong dòng nhận:

```text
receipt_line_id -> purchase_order_line_id -> purchase_order_id, book_id
```

purchase_order_line_id có thể được nhận qua nhiều dòng/phiếu, nên nó không phải khóa của GoodsReceiptLine. PO ID và Book ID là thông tin phụ thuộc bắc cầu. Bản mới chỉ lưu receipt_line_id, receipt_id, purchase_order_line_id, received_quantity. View `vGoodsReceiptSources` JOIN sang PurchaseOrderLine khi cần PO/Book.

Không thêm bảng GoodsReceiptPO chỉ để lưu lại đúng liên kết đã suy ra từ các dòng nhận. Nếu sau này cần đăng ký PO dự kiến nhận trước khi có dòng thực nhận, đó là một fact khác và phải được đặc tả riêng.

### Trả hàng theo dòng bán gốc

```text
return_line_id -> return_id -> original_order_id, returned_at_utc
return_line_id -> original_order_line_id -> order_id, book_id
```

BookReturnLine giữ return_id và original_order_line_id nhưng không chép Book ID, Order ID, giá bán gốc hoặc tên sách. BookReturn giữ đơn bán gốc và thời điểm trả; SalesOrderLine giữ Book và thông tin dòng bán. Trigger bảo vệ sự nhất quán giữa hai đường tham chiếu đến đơn bán, không thêm cột trùng chỉ để ghép composite FK.

Số tiền refund là **số tiền thực hoàn được ghi nhận cho dòng trả**, không phải công thức mặc định quantity × current_price. Vì quy tắc phân bổ giảm giá/làm tròn chưa có, không tạo calculated column suy đoán giá trị hoàn. Phương thức hoàn luôn cash theo quy tắc hiện tại nên không lặp một cột method có giá trị hằng trên mọi phiếu.

### Count và chọn lần đếm

CountObservation chứa các lần đếm; CountSelection có một kết quả cho mỗi Count/Book. UNIQUE selected_observation_id cùng FK ghép chứng minh observation được chọn thực sự thuộc đúng Count/Book. UNIQUE bổ sung trên (observation_id,count_id,book_id) của bảng Observation chỉ là superkey đích cho SQL FK; khóa ứng viên tối thiểu của Observation vẫn là observation_id.

recorded_quantity trong Selection là **mốc tồn lịch sử dùng để đối chiếu**, không phải bản sao được giữ đồng bộ với tồn hiện tại. Chênh lệch được tính qua view. Khi posting phải phát hiện biến động kho sau lúc đếm hoặc khóa/chốt mốc phù hợp; schema chưa tự giải quyết việc bán/nhập diễn ra đồng thời.

### Role metadata

Employee chỉ giữ role_code. Không chép role_name/description vào mỗi Employee nếu có FD `role_code -> role_name, description`. Nếu cần quản lý các metadata đó trong DB, thêm Role lookup; nếu tập role cố định, nhãn hiển thị có thể nằm trong ứng dụng. Không tự khôi phục bộ Role–EmployeeRole–RolePermission nhiều-nhiều cũ.

Với Account 1:1, employee_id là khóa ứng viên nếu UNIQUE + NOT NULL được áp dụng. Vì vậy, chép một thuộc tính Employee vào Account **không tự động là vi phạm 3NF bên trong Account**, dù vẫn có rủi ro lệch dữ liệu giữa hai bảng. Kiểm tra mọi khóa ứng viên, không chỉ PK đã chọn.

## Chứng minh và giới hạn của kết quả

`build_review.py` liệt kê FD theo khóa và các determinant đã khai báo, tính bao đóng, tìm khóa ứng viên tối thiểu, rồi xét mọi tập determinant. Điều kiện 3NF: với mỗi FD không tầm thường X → A, X là superkey hoặc A thuộc một khóa ứng viên. 38 quan hệ qua kiểm tra dưới tập FD này; **một bảng có ID không vì thế tự được chứng nhận 3NF khi còn FD nghiệp vụ chưa khai báo**.

`decomposition-proof.json` chứa năm ví dụ phân rã: Employee/grant, Receipt/PO line, Return/header/original line, Count/header/observation và Adjustment theo Book. FD tableau chase xác nhận lossless, và phép chiếu FD + bao đóng xác nhận bảo toàn phụ thuộc trên các ví dụ đó. Checker còn kiểm tra các phản ví dụ: Role phụ thuộc bộ phận, PO-line phụ thuộc bắc cầu, phân rã mất dữ liệu và phân rã làm mất phụ thuộc.

Đây không phải chứng minh mọi dữ liệu cũ có thể chuyển tự động. Contact/discount là chuỗi mơ hồ; header Adjustment thiếu phân bổ; Book trùng chưa có occurrence identity; Receipt cũ không chỉ rõ dòng PO; Payment/Provider/Shipment còn cardinality cần xác nhận. Các mối liên kết nguồn chưa chốt được giữ có ghi chú **PROVISIONAL**, không biến thành yêu cầu M:N đã được chấp thuận.

## Ràng buộc Return đã có và phần chưa có

- FK đến đúng phiếu trả và dòng bán gốc; một phiếu trả thuộc một đơn bán.
- CHECK số lượng trả dương, tiền hoàn không âm; đây là constraint của bản đề xuất.
- Trigger chặn dòng trả từ đơn khác và tổng số lượng trả của dòng bán vượt số đã mua, kể cả một statement có nhiều dòng.
- Trigger chặn đổi đơn trên header Return hoặc giảm/chuyển dòng bán khiến các phiếu trả cũ sai.
- Parent sales-line locks được dùng khi kiểm tra trả đồng thời; **chưa chạy load/concurrency/deadlock tests**. Application phải xử lý transaction/retry và không coi kiểm tra đơn luồng là chứng minh đồng thời.

Chưa có UI/API/service trả hàng, xét thời hạn/tình trạng sách, công thức refund sau giảm giá, ghi chi tiền hoặc transaction nhập lại kho. Những hành vi này không được suy ra từ trigger. Một Return đã lưu trong mô hình này là Return được nhận; draft/rejected request chưa được mô hình hóa.

## Tồn hiện tại và lịch sử

BookStock phục vụ truy vấn nhanh; InventoryMovement lưu biến động với posting_key duy nhất. Không tự ghi ledger từ Audit, Count hoặc Return bằng trigger vì quy trình posting/cancellation và mốc tồn chưa đầy đủ. Luồng ứng dụng phải cập nhật tồn + movement trong cùng transaction, dùng cùng posting_key cho retry và kiểm tra nguồn chứng từ. Với Count, không hạch toán Count và Adjustment thành hai biến động cho cùng một chênh lệch.

`vStockReconciliation` đối chiếu ledger và các dòng BookStock hiện có; opening balance phải có movement phù hợp trước khi coi ledger là đầy đủ. View không chứng nhận mọi Book đã được provision dòng stock. Chênh lệch số học dùng SUM các giá trị có dấu để giữ đủ scale 10 của decimal(38,10), tránh giảm scale khi trừ trực tiếp ở precision tối đa.

## Các điểm nghiệp vụ còn mở

| Điểm | Ảnh hưởng còn lại |
|---|---|
| Role đặt ở đâu theo ý PO; ai được cấp Admin | Reconcile thiết kế và ma trận quyền trước khi triển khai authorization |
| Thời hạn/tình trạng nhận trả; refund allocation/rounding | Không thể tự tính hoặc tự duyệt Return hoàn chỉnh |
| Payment–Order cardinality, số tiền, settlement/QR | OrderPayment hiện là link provisional; không có mô hình quyết toán hoàn chỉnh |
| PO–Provider, shipment ownership/tracking namespace | Không chứng nhận các pair link đang giữ là cardinality cuối cùng |
| Contact kind, discount meaning, quantity units, tiền tệ | Miền/constraints còn là lựa chọn kỹ thuật của bản review |
| Count posting concurrency, manual Adjustment, opening stock | Chưa có transaction nghiệp vụ hoàn chỉnh |
| Audit approval evidence, cấu hình/alerts, cost/profit reports | Không tự thêm cấu trúc chưa có thuộc tính và quy tắc đủ rõ |

## Kết quả kiểm tra local

Các kết quả zero-row/rollback dưới đây thuộc mốc dựng schema trước khi seed. Đợt sau đã nạp 223 dòng fixture theo yêu cầu người dùng; xem [dữ liệu demo](demo/README.md). Kiểm tra catalog sau seed so sánh 38 bảng nghiệp vụ và bỏ qua bảng hỗ trợ SSMS `sysdiagrams`, không xóa/sửa bảng đó.

SQL Server 2019 local `DESKTOP-I74VCAM` đã parse/chạy DDL trên database review riêng. Catalog có 38 bảng, 125 cột, 38 PK, 6 UNIQUE, 40 FK, 8 CHECK, 4 view, 3 trigger; FK/CHECK đều hoạt động và trusted. Bộ kiểm tra SQL qua 38/38 trường hợp, gồm các trường hợp đúng/sai của receipt, recount, account, adjustment và Return, cùng kiểm tra độ chính xác decimal. Dữ liệu giả rollback hết; IDENTITY có thể tăng do test.

Snapshot metadata/row count DEV_DRAFT trước/sau không đổi; SHA-256 file SQL gốc không đổi. Chứng cứ local trong `.artifacts/database-normalization-20261027/`, không phải CI. Không thay connection string hay migrations của ứng dụng và không push các artifact mới trong đợt này.

## Tham khảo ERP theo yêu cầu của người dùng

Microsoft Business Central hỗ trợ nhận một phần số lượng PO; Dynamics 365 có nhận nhiều PO trong một lần xử lý. Điều này không buộc project phải chọn một cardinality cụ thể; lựa chọn phiếu nhận gộp nhiều PO ở đây đến từ quyết định trực tiếp của người dùng. [Business Central](https://learn.microsoft.com/en-ca/dynamics365/business-central/purchasing-how-record-purchases), [Dynamics 365 product receipt](https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/product-receipt-against-purchase-orders).

ERP phân biệt phạm vi đếm và các bản ghi kiểm kê; không thể suy ra phải cộng chỉ vì hai dòng có cùng mã hàng. Project đã chọn lưu các lần đếm lại và chọn một kết quả cuối. Không thêm warehouse/location/lot/serial vào phạm vi chỉ vì hệ thống ERP khác hỗ trợ chúng. [Dynamics 365 inventory journals](https://learn.microsoft.com/en-us/dynamics365/supply-chain/inventory/inventory-journals), [Odoo inventory adjustments](https://www.odoo.com/documentation/18.0/applications/inventory_and_mrp/inventory/management/inventory_adjustments.html).
