# Dữ liệu demo cho các luồng chính

Dataset giả lập ngày 27/10/2026 cho **Book Sales Management System**, đã nạp vào database local `SWD392_BookSales_3NF_REVIEW_20261027`. Có **223 dòng dữ liệu trên 36 bảng**, gồm 12 sách với tổng tồn **183 cuốn**. Promotion/PromotionBook không được seed vì không thuộc các luồng chính đã chọn và quy tắc khuyến mãi còn mở.

Toàn bộ tên, giá, số lượng, ngày giao dịch, trạng thái và grant là dữ liệu minh họa. Chúng không bổ sung quy tắc nghiệp vụ đã được PO duyệt. Đơn hàng đều dùng Cash, không có giảm giá trong các giao dịch mẫu. Log external được ghi rõ `DEMO/SWD392/...` và `fixture-only:no-network`, không có yêu cầu VnPay/email/vận chuyển thật.

## Bắt đầu

Trong SSMS, chọn review database và chạy [demo_queries.sql](demo_queries.sql). File chỉ SELECT dữ liệu, chia sẵn theo từng luồng. ID chuỗi bắt đầu bằng `DEMO-20261027-`; IDENTITY của dòng giao dịch được SQL Server cấp, không giả định bắt đầu bằng 1. Tra cứu dòng theo parent ID/Book/số lượng hoặc kết quả truy vấn, không hard-code IDENTITY.

PowerShell tại repository root:

```powershell
# Nạp lần đầu; các lần sau trả ALREADY_SEEDED, không ghi đè dữ liệu đã chỉnh:
.\database\normalized-review-2026-10-27\demo\seed_demo.ps1

# Kiểm tra đúng snapshot ban đầu, trước khi thực hiện thao tác demo:
.\database\normalized-review-2026-10-27\demo\seed_demo.ps1 -VerifyBaseline
```

Có thể mở [seed_demo.sql](seed_demo.sql) để chạy trực tiếp trên SSMS. Script có transaction, khóa chống hai lần seed đồng thời, kiểm tra namespace và marker phiên bản ở extended properties. Nếu namespace đã có dữ liệu nhưng chưa có marker phù hợp, script báo lỗi thay vì sửa/xóa dữ liệu. Marker và toàn bộ dữ liệu được commit cùng nhau. Rerun không reset các thao tác demo và không tự phục hồi những dòng bị xóa thủ công. Không có script dọn dữ liệu tự động.

`build_demo.py` dùng Python standard library để tái tạo SQL/manifest, không kết nối DB. [demo-manifest.json](demo-manifest.json) chứa fixture, số dòng và các kết quả gốc. Chứng cứ chạy nằm ở `.artifacts/demo-data-20261027/`, gitignored; bản chạy lần hai đã được đối chiếu digest toàn bộ nội dung bảng để xác nhận không đổi.

## Dataset cho từng luồng

| Thành viên trong đề xuất phân công | Luồng | Dữ liệu dùng để bắt đầu |
|---|---|---|
| Nam | UC10 Update Price | `DEMO-20261027-B01`, giá hiện tại 130.000; có thể chọn 135.000 làm giá mới cho bài demo |
| Kiệt | UC07 Record Inventory Count | `DEMO-20261027-COUNT-READY`: B04 có tồn hiện tại 13, hai lần đếm 11 và 12; chưa có kết quả được chọn/Adjustment |
| Hưng | UC01 Cash sale | B06 giá 120.000, tồn 12; thêm hai cuốn tạo ví dụ đơn 240.000. B11 tồn 0 và B12 tồn 1 để thử validation sau khi chốt quy tắc bán vượt tồn |
| Hùng | UC12 permission update | Sales A có grant UC21; Sales B chưa có. Có Admin, Manager và account mẫu bị khóa để chuẩn bị các trường hợp truy cập |
| Tech Lead | UC15 inventory report | 12 sách, tổng tồn 183; bảng sách/tồn/giá trong demo_queries.sql |
| Phần Return của Hưng | UC21 Book Return | `DEMO-20261027-SALE-1` có hai dòng cùng B01; một dòng đã trả hết qua hai phiếu, dòng còn lại còn một cuốn có thể trả |

Các thao tác trong bảng là **kịch bản để triển khai/thử luồng sau này**, không phải các service/UI đã chạy. Giá mới, đơn bán mới, lần chọn Count và grant mới chưa được áp dụng vào fixture ban đầu. Quyền của Manager khi sửa grant cho nhân viên vẫn phải theo phạm vi được chốt; dữ liệu mẫu không tự cấp quyền đó cho chương trình.

## Account mẫu

| Username | Role | Ví dụ trạng thái/grant |
|---|---|---|
| `swd392_demo_admin` | SYSTEM_ADMIN | ACTIVE; có đủ 21 grant demo |
| `swd392_demo_manager` | STORE_MANAGER | ACTIVE; UC10–UC15 |
| `swd392_demo_inventory` | INVENTORY_STAFF | ACTIVE; UC03–UC09 |
| `swd392_demo_sales_a` | SALES_STAFF | ACTIVE; UC01, UC02, UC21 |
| `swd392_demo_sales_b` | SALES_STAFF | ACTIVE; UC01, chưa có UC21 |
| `swd392_demo_disabled` | SALES_STAFF | DISABLED; có UC01 để minh họa trạng thái account vẫn phải được kiểm tra |

Đây là **record nghiệp vụ**, không phải SQL Server login hoặc credential ứng dụng. Schema hiện chưa có cơ chế mật khẩu/đăng nhập; không có password để dùng và không seed mật khẩu plaintext. Grant UC02 chỉ là mẫu dữ liệu cho quyền; không có QR payment thật. Ma trận grant này là fixture phục vụ demo, không thay thế quyết định quyền của PO hoặc việc enforcement phía server.

## Kiểm kê và tồn kho

- `COUNT-HISTORY`: B01 được đếm 42 rồi 43, chọn **43** so với baseline 44 → chênh lệch -1. B03 chọn 18 so với 19 → -1. B02 chọn 27 so với 27 → 0.
- Một `ADJUSTMENT-COUNT-HISTORY` có hai dòng B01/B03. Ledger chỉ hạch toán qua Adjustment một lần, không có movement Count cộng thêm lần nữa.
- `COUNT-READY`: lưu hai observation B04=11/12, chưa selection hoặc posting. Chọn 12 sau này sẽ có chênh lệch -1 so với tồn 13; thao tác cập nhật tồn/lịch sử cần transaction của ứng dụng.
- `COUNT-NOCHANGE`: B07 chọn 24, baseline 24, không có Adjustment.
- Audit mẫu có status `APPROVED` và kết quả đếm B01=40, trong khi tồn vẫn 43. Nó không tự sinh Adjustment hay thay tồn.

Tồn cuối ban đầu của B01–B12 lần lượt là **43, 27, 18, 13, 8, 12, 24, 5, 18, 14, 0, 1**. Có 29 movement gồm opening, nhận hàng, bán, trả và adjustment; tất cả chênh lệch trong `vStockReconciliation` bằng 0. Đây là snapshot được seed trực tiếp, không chứng minh code posting đã triển khai.

## Bán hàng và trả hàng

Có ba Cash order: SALE-1 tổng 540.000, SALE-2 tổng 560.000, SALE-3 tổng 345.000; tổng các đơn mẫu 1.445.000. SALE-3 không gắn Customer để minh họa cột customer_id đang nullable trong bản schema review.

SALE-1 có các dòng B01 × 2, B01 × 1 và B02 × 1. RETURN-1 và RETURN-2 mỗi phiếu trả một cuốn từ **dòng B01 × 2**, refund mỗi lần 130.000. Tổng tiền hoàn được ghi nhận là 260.000; hai cuốn đã được thêm vào ledger/tồn. Dòng B01 × 1 chưa trả, còn một cuốn cho bài demo Return tiếp theo.

`demo_queries.sql` hiển thị `order_line_id`, số đã mua, đã trả và còn có thể trả của từng dòng. Dùng đúng ID của dòng còn một cuốn, không chọn ngẫu nhiên theo Book ID. Trigger sẽ chặn trả thêm từ dòng đã hết số lượng, nhưng điều kiện thời hạn/tình trạng sách và công thức tiền hoàn vẫn cần code/đặc tả riêng. Mức refund trong fixture là ví dụ đơn không giảm giá, không chốt chính sách refund chung.

## Nhận hàng và báo cáo

Có bốn PO và tám dòng PO; PO-1 có hai dòng B01 riêng. GR-1 nhận từ PO-1 và PO-2. PO-1 tiếp tục nhận ở GR-2; PO-2 tiếp tục ở GR-2/GR-3. PO-3/PO-4 chưa có receipt, để chuẩn bị tạo phiếu nhận tiếp theo. Ví dụ này không chốt quy tắc một PO có bao nhiêu Provider hoặc có cho gộp khác Provider trên một receipt.

Báo cáo baseline có **12 đầu sách, 183 cuốn**. Tổng `current_price × current_quantity` là **27.555.000** theo giá catalog giả lập. Đây là giá trị hiển thị để kiểm tra phép tính, không phải giá vốn, lợi nhuận hay định giá kế toán.

Sau khi đổi giá, bán thêm, nhận thêm, sửa grant hoặc chốt Count/Return mới, kiểm tra baseline có thể báo khác — đó là dữ liệu demo đã thay đổi. Script seed vẫn giữ nguyên các sửa đổi đó. Các bộ test chuẩn hóa trước đây yêu cầu database rỗng; không chạy chúng trên database đã có fixture. Dùng verify_demo.sql để kiểm tra snapshot ban đầu và truy vấn reconciliation để theo dõi tồn sau các thao tác.
