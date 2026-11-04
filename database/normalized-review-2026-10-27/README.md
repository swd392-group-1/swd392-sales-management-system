# Bản thiết kế database chuẩn hóa để review

**Cập nhật 02/11/2026 — lưu mật khẩu:** đã bổ sung `Account.password_hash nvarchar(512) NULL` vào generator, schema và SSMS export. Tổng số cột hiện là **126**; các số 125 bên dưới là kết quả lịch sử trước sửa. Với DB review đang có, chạy `05_add_account_password_hash.sql`; script có thể chạy lại, không thay username/quyền/trạng thái và không đặt mật khẩu mặc định. Sáu account demo hiện chưa được cấp mật khẩu. `NULL` nghĩa là chưa thiết lập thông tin xác thực; code Login phải từ chối đăng nhập cho đến khi module phụ trách lưu hash hợp lệ. Chỉ lưu hash do password hasher của ứng dụng sinh, không lưu mật khẩu rõ hoặc SQL `HASHBYTES` thay cho password hasher. Đây là sửa nơi lưu credential, chưa triển khai Login/đặt mật khẩu. JWT ngoài phạm vi.

Ngày 27/10/2026. **PROPOSAL — not yet approved.** Bản này rà soát toàn bộ 27 bảng của `SWD392_BookStore.sql`, xây dựng schema đề xuất theo các quyết định mới và bổ sung dữ liệu hỗ trợ trả hàng. Các quan hệ đạt kiểm tra 2NF/3NF **theo tập phụ thuộc hàm đã khai báo**, chưa phải chứng nhận đã biết đầy đủ mọi quy tắc của nghiệp vụ.

Database review đã được tạo riêng trên SQL Server local: **`SWD392_BookSales_3NF_REVIEW_20261027`**. Có 38 bảng, 125 cột, 38 PK, 6 UNIQUE, 40 FK, 8 CHECK, 4 view và 3 trigger. Database `SWD392_BookSales_DEV_DRAFT` và file export gốc được giữ nguyên. Đây là tạo schema mới để so sánh, không phải migration dữ liệu đang chạy.

Sau đợt kiểm tra schema, theo yêu cầu người dùng đã nạp **223 dòng dữ liệu demo**, gồm sách/tồn, Cash sale, Return, nhận hàng, recount và grant mẫu. Xem [hướng dẫn dữ liệu theo luồng](demo/README.md). Kết quả “zero business rows” trong báo cáo dựng schema là mốc trước khi seed. SSMS có thể tạo bảng hỗ trợ `sysdiagrams`; kiểm tra catalog giữ nguyên bảng đó và không tính nó vào 38 bảng nghiệp vụ.

## Các quyết định đã áp dụng

| Quy tắc đã xác nhận | Biểu diễn trong bản đề xuất |
|---|---|
| Một nhân viên có một account và một role chính | Bỏ EmployeeAccount; Account tham chiếu Employee bằng FK NOT NULL + UNIQUE; Role đặt một lần trên Employee |
| Admin quản trị toàn bộ quyền; Manager quản lý nghiệp vụ | Có mã role và danh mục/grant permission; chính sách kiểm tra quyền phải triển khai trong application, không phải SQL GRANT cho người dùng nghiệp vụ |
| Một PO có thể nhận nhiều đợt; một phiếu nhận gộp nhiều PO | GoodsReceiptLine tham chiếu chính xác PurchaseOrderLine; PO của phiếu nhận được truy vấn từ các dòng |
| Book được lặp trong Sale/PO/Receipt | Mỗi dòng có identity riêng, không dùng parent_id + book_id làm PK |
| Dòng Book lặp trong Count là đếm lại | Lưu toàn bộ InventoryCountObservation; InventoryCountSelection chọn một kết quả cho mỗi Count/Book, không SUM các lần đếm |
| Lưu tồn hiện tại và lịch sử | BookStock và InventoryMovement; cập nhật đồng bộ bằng transaction là trách nhiệm của luồng posting sẽ triển khai |
| Một Count có chênh lệch tạo một Adjustment gồm các Book lệch | CountAdjustment liên kết tối đa một Adjustment/Count; quantity_change đặt trên AdjustmentBook |
| Audit và phê duyệt Audit không tự thay tồn | InventoryAudit/Line được tách khỏi Count; không có trigger thay tồn khi Audit đổi trạng thái |
| Trả một phần, nhiều lần; từng dòng theo dòng bán gốc | BookReturn → SalesOrder; BookReturnLine → SalesOrderLine; trigger kiểm tra đúng đơn và giới hạn số lượng trả tích lũy |
| Sách được nhận trả được nhập lại, hoàn tiền mặt | Dòng trả lưu số lượng và số tiền thực hoàn; tiền mặt là chính sách cố định của luồng, không thêm tích hợp hoàn VnPay |

Vị trí Role trên Employee khác câu ghi chú PO “Role ở EmployeePermission”. Đây là phương án sửa phụ thuộc bộ phận để review với PO, không coi lời ghi chú đó đã được thay đổi chính thức. Manager được cấp quyền nào, được sửa quyền của ai, ai đổi role Admin vẫn cần ma trận quyền cụ thể.

## File để đọc và chạy

| File | Mục đích |
|---|---|
| [01_create_database.sql](01_create_database.sql) | Chỉ tạo database mới; báo lỗi nếu tên đã tồn tại |
| [02_create_schema.sql](02_create_schema.sql) | Tạo bảng và ràng buộc; từ chối target không rỗng |
| [03_views_and_return_rules.sql](03_views_and_return_rules.sql) | View tra cứu dữ liệu dẫn xuất và trigger bảo vệ trả hàng |
| [04_verify_schema.sql](04_verify_schema.sql) | Kiểm tra catalog, ràng buộc hoạt động/trusted |
| [Báo cáo chuẩn hóa](normalization-report.vi.md) | Grain, khóa, FD, phân rã, thay đổi và phần cần review |
| [normalization-matrix.csv](normalization-matrix.csv) | Khóa ứng viên và FD cho cả 38 quan hệ, mở được bằng Excel |
| [schema-manifest.json](schema-manifest.json) | Cột/kiểu/khóa/FK/CHECK và ghi chú của từng quan hệ |
| [source-to-candidate.json](source-to-candidate.json) | Ánh xạ đủ 27 bảng nguồn sang thiết kế mới |
| [fd-proof.json](fd-proof.json), [decomposition-proof.json](decomposition-proof.json) | Kết quả bao đóng FD, khóa tối thiểu, kiểm tra lossless/dependency preservation trên các ví dụ đã khai báo |

Trên SSMS, kết nối SQL Server local và mở database review đã tạo. Không chạy lại script tạo database trên database có sẵn. Khi dựng trên một server mới, chạy 01 → 02 → 03 → 04, dừng ngay khi có lỗi. Script không tự DROP hay ghi đè database.

PowerShell, từ repository root:

```powershell
# Chỉ dùng khi review database chưa tồn tại:
.\database\normalized-review-2026-10-27\execute_review.ps1

# Database review đã tồn tại trên máy hiện tại:
.\database\normalized-review-2026-10-27\execute_review.ps1 -VerifyOnly
.\database\normalized-review-2026-10-27\test_review.ps1
```

Database hiện đã có demo data: không chạy bộ `test_review.ps1` yêu cầu database rỗng. Dùng `demo/seed_demo.ps1 -VerifyBaseline` để kiểm tra snapshot demo ban đầu. `execute_review.ps1 -VerifyOnly` kiểm tra cấu trúc mà không xóa dữ liệu; nếu chạy thêm Python `verify_catalog.py` sau seed, truyền `--allow-data`.

Các script dùng Windows Authentication và System.Data.SqlClient có sẵn trong Windows PowerShell. Tham số `-Server` cho phép chỉ rõ server local đã chọn. `execute_review.ps1` mặc định còn đọc snapshot database DEV_DRAFT để đối chiếu; nếu máy khác không có DEV_DRAFT, dùng bộ SQL thủ công cho database mới và không tuyên bố đã kiểm tra bảo toàn database cũ.

`test_review.ps1` yêu cầu review database rỗng. Nó tạo dữ liệu giả trong từng transaction rồi rollback, không xóa dữ liệu sẵn có; identity có thể tăng dù transaction rollback. Trên máy này đã qua **38/38 kiểm tra SQL**. Chứng cứ local ở `.artifacts/database-normalization-20261027/` được gitignore. Chưa có CI hoặc kiểm tra giao diện của các luồng này.

## Phạm vi còn cần hoàn thiện

Schema này có dữ liệu và ràng buộc cho Return, chưa phải UI/service trả hàng hoàn chỉnh. Thời hạn/tình trạng sách được trả, phân bổ giảm giá/làm tròn tiền hoàn, việc ghi tiền mặt và nhập lại kho trong cùng transaction vẫn phải được đặc tả/triển khai. Trigger hiện bảo vệ đúng đơn bán và số lượng, không xác nhận điều kiện nhận trả hay tính hộ tiền hoàn.

Tương tự, BookStock + InventoryMovement chưa tự động cập nhật khi bán/nhập/đếm/trả. Posting phải dùng khóa idempotency ổn định cho cùng một sự kiện, kiểm tra nguồn chứng từ, cập nhật tồn và lịch sử trong cùng transaction. UNIQUE posting_key chỉ từ chối cùng khóa hai lần; nó không chặn caller dùng hai khóa khác nhau cho cùng một sự kiện. Với Count, chỉ hạch toán chênh lệch một lần qua Adjustment, không thêm một movement Count rồi thêm lại movement Adjustment.

Không thay SRS, Chen, tracker, cấu hình kết nối của app hoặc EF migrations trong đợt này. Không push các file database mới lên GitHub trong đợt chuẩn hóa này. Nguồn quyết định chuẩn vẫn là `Project Context/SWD392_PROJECT_CONTEXT.md` ở workspace cha.
