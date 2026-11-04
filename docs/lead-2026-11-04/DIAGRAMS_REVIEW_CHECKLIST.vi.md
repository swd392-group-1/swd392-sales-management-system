# Danh sách chỉnh diagram để hoàn thiện SRS

Đã đọc bản Use_Case.drawio từ Drive 1h2YFx_c8Ph-Nxb2cYtIO_pG7um5lv1rv ngày 04/11/2026, so với bản local APPROVED_REVIEW và quyết định trong chat. Không sửa file diagram gốc. Context/Chen trên Drive được nhận diện metadata; đánh giá nội dung dùng các bản XML/JPG local và tài liệu Thống nhất nghiệp vụ trên Drive. Không tuyên bố byte hai bản Context cloud/local giống nhau.

| Trang | Việc cần tự chỉnh | Cơ sở / lưu ý |
|---|---|---|
| Sales Staff | Thêm UC21 Process Book Return, association Sales Staff — UC21 và include Login theo quy ước GV | Không nối Return extend/include Process Sale. Không thêm Inventory Staff vào UC21 chỉ vì có tăng tồn |
| Sales Staff | Kiểm tra nhãn extend UC02 → UC01 và điều kiện Payment Method = QR | Edge đã tồn tại trên Drive nhưng nhãn stereotype nằm ở cell con, cần đảm bảo xuất ra đọc rõ. QR không là UC độc lập |
| Sales Staff | Bổ sung include Login cho UC02 nếu tiếp tục quy ước áp dụng cho từng UC | Drive hiện có include từ UC01, thiếu từ UC02; local APPROVED_REVIEW đã có |
| Store Manager | Bổ sung association tới chức năng xem log của Sales Staff/Inventory Staff | Quyền xem log đã được người dùng xác nhận. Có thể dùng lại UC Review System Activity Logs/Review Security Audit Logs với giới hạn đối tượng trong specification, không tự tạo UC trùng tên |
| System Admin | Làm rõ Admin được quản lý tất cả quyền, không chỉ Account | Dùng lại UC12 với Admin actor hoặc ghi rõ actor phụ/quyền bao phủ trong mô hình thống nhất; giữ Manager quản lý Business permissions. Không thêm Permissions chung vào Context khi chưa xác nhận cách thay luồng gốc |
| Login | Kiểm tra actor của Login dùng chung và mô tả username/password; bỏ JWT | Không bắt buộc vẽ logout/change password thành UC mới chỉ vì thông lệ. Chỉ thêm khi phạm vi được xác nhận; demo change-password cũ không phải yêu cầu được duyệt |
| Inventory Staff | Giữ Count độc lập với Audit; không thêm include/extend chỉ vì cùng dữ liệu | Các UC03–09 đã hiện diện. Multi-PO receiving/recount là quy tắc trong specification/ERD, không tự sinh UC mới |
| Store Manager | Giữ UC15 View Management Reports; nêu tồn kho là luồng code được chọn | Không cần thêm UC Export CSV riêng nếu chỉ là thao tác trong UC15; chưa thể mô tả profit formula khi thiếu giá vốn |
| Context | Review bổ sung trao đổi Return của Sales Staff và kết quả hoàn tiền mặt | Chức năng đã chốt sau bản Context gốc. Không thêm actor Customer/Provider, không gọi VnPay refund; không tự bỏ 40 luồng đang có |
| Chen | Bổ sung Return/return line identity và Inventory Count/recount semantics ở mức khái niệm | Phân biệt dòng bán cùng Book; một Return thuộc một Order; Count observations chọn một kết quả. Không sao chép 38 bảng SQL thành Chen |
| Chen Account | Bổ sung ý nghĩa credential/role chính phù hợp dữ liệu mới | Role là thuộc tính cố định trên Employee trong schema review; hash thuộc Account. Bảng liên kết kỹ thuật không tự thành conceptual entity |
| Database design 3.4 | Dùng schema 38 bảng/126 cột cùng FD, candidate keys và phân rã | Tách khỏi conceptual Chen; proof 3NF có điều kiện theo FD đã khai báo, chưa chứng nhận mọi business rule đã đủ |

Không thêm hàng loạt UC cho CRUD, kiểm tra số lượng, tăng tồn, hoàn tiền, in phiếu hoặc từng bước kỹ thuật. Những hành vi này có thể nằm trong UC specification. Chỉ tách UC khi có mục tiêu actor độc lập được xác nhận.

Các điểm chưa quyết định đủ để tự vẽ luồng hoàn chỉnh: discount/refund có khuyến mãi, profit/cost basis, nhận vượt PO/cùng nhà cung cấp, hủy phiếu đã posting, quy tắc concurrency Count, log retention/alert threshold và API Google Email/ViettelPost/VnPay thật. SRS phải giữ nhãn thiếu yêu cầu ở các điểm đó.
