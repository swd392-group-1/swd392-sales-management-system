# Phần Tech Lead và kết quả tích hợp ngày 04 tháng 11 năm 2026

Luồng cá nhân UC15: xem báo cáo tồn, lọc mã/tên và sách còn tồn, xuất CSV. Nguồn Book + BookStock hiện tại; giá hiện tại không được gọi là giá vốn/lợi nhuận. Export truy vấn lại tại thời điểm yêu cầu nên có thể khác màn hình nếu tồn đã đổi. Thiếu BookStock hiển thị chưa có dữ liệu; không tự chuyển thành tồn 0.

## Chạy và demo

Dùng .NET SDK 8; restore/build/test BookSales.sln. Cấu hình ConnectionStrings__BookSales bằng biến môi trường hoặc appsettings.Local.json (không commit). Ví dụ local dùng Windows Authentication: Server=localhost;Database=SWD392_BookSales_3NF_REVIEW_20261027;Integrated Security=True;TrustServerCertificate=True. Chạy Web trong Development. Không có EnsureCreated/Migrate tự động.

1. Mở /reports/inventory: dữ liệu demo ban đầu có 12 sách, 183 đơn vị.
2. Lọc B06 và chỉ sách còn tồn; xuất CSV theo cùng bộ lọc. Có UTF-8 BOM, escape CSV và chống công thức từ mã/tên sách.
3. Mở /sales ở cửa sổ khác; mở /Pricing để thay giá một sách. Commit DB trước khi enqueue; PriceDispatchWorker đọc Channel rồi gửi BookPriceChanged qua /hubs/prices. Màn bán thông báo thay đổi, không tự ghi đè đơn đang nhập; người dùng tải lại danh mục. Không tuyên bố đã chốt chính sách đổi giá giỏ hàng.

## Giải thích code và OOP

- ReportsController nhận request, gọi service và chọn view/file. Không viết SQL trong controller.
- InventoryReportService chuẩn hóa/kiểm tra bộ lọc, điều phối query và tạo kết quả.
- IInventoryReportQuery là abstraction; EfInventoryReportQuery hiện thực bằng EF Core. Có thể thay bằng fake trong test mà không đổi service: interface và polymorphism qua DI.
- InventoryReport sao chép danh sách vào collection chỉ đọc, có getter tổng số lượng/thiếu dữ liệu: encapsulation. Đây là query result, không tạo bảng Report.
- InventoryCsv chịu trách nhiệm định dạng file. Book.ChangeCurrentPrice bảo vệ quy tắc giá khi gọi hành vi đổi giá; Book hiện vẫn có public setters để tương thích mapping/module bán, chưa phải entity đóng kín toàn bộ.
- Không thêm inheritance chỉ để minh họa. Interface implementation đã thể hiện quan hệ hiện thực trên Class Diagram.
- await EF query/commit là I/O bất đồng bộ trong request; request vẫn chờ kết quả. Queue/worker là xử lý tách khỏi request; SignalR là realtime tới client đang kết nối.

## PR được kiểm tra

- #13 head 168e75b5e3f6c3c89c721a6cae44be82e0ee646c: 41 test local đạt trước ghép. Có bán cash, QR mô phỏng Development, trả sách/hoàn tiền mặt/transaction kho. Không gọi QR là VnPay thật. PR description còn mẫu trống.
- #14 head 9c433cdcff7df415b6c4b121a6c013a14de1ac4d: phát hiện SDK version rỗng, Book/DbContext trùng với #13, mapping BOOK/column không khớp schema, queue mock và cấu hình bắt buộc DB làm mất foundation behavior. Đã resolve bằng mapping của #13, giữ hành vi ChangeCurrentPrice, sửa nullable event price, khôi phục SDK 8, dùng queue/worker thật.
- Loại cấu hình server cá nhân và mật khẩu mẫu khỏi appsettings hiện tại. Cấu hình local đặt ngoài Git.
- Branch feature/hung-access-audit và kiet/inventory-module tồn tại nhưng không thuộc hai PR đang mở. Inventory branch còn đổi cấu trúc solution; không coi những branch này là đã review/merge trong đợt hai PR. Cần PR riêng để tích hợp có kiểm tra.

## Kiểm chứng và giới hạn

44/44 test Release local đạt; HTTP report/export/filter/sales/returns/pricing đạt với SQL local; hai WebSocket receiver nhận cùng BookPriceChanged sau commit. Dữ liệu baseline giữ 12 sách/183 tồn, B01 giá 130000. CI status hai head không có check được báo cáo; không gọi test local là CI.

Các màn nghiệp vụ/hub giá chỉ mở local Development đến khi access module được tích hợp. Chưa tích hợp quyền Manager/Admin trong worktree này. Cookie Login là hướng đã đề xuất; JWT ngoài phạm vi. Không coi đây là hệ thống production hoặc mọi UC đều đã code. Queue nằm trong bộ nhớ, không durable/replay/retry: mất tiến trình có thể mất event, lỗi enqueue sau commit không hoàn tác được giá. Client phải đọc lại DB sau reconnect; thông báo không phải nguồn dữ liệu chuẩn.

Các diagram inventory-report-class/sequence và architecture có nguồn PlantUML cùng SVG/PNG mới trong thư mục này. Diagram cũ ở workspace không được sửa.
