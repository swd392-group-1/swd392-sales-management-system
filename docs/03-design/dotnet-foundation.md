# Hướng dẫn khung .NET 8

Ngày: 27/10/2026. Trạng thái: code nền tảng local, cần nhóm review. Nguồn quyết định: người dùng/Tech Lead đồng ý C# ASP.NET Core MVC, ba tầng, SQL Server, SignalR và yêu cầu **chỉ dùng .NET 8**. Không coi lựa chọn công nghệ là duyệt các yêu cầu nghiệp vụ còn thiếu.

## Kiến trúc đang có

Một ứng dụng deploy chung, bốn assembly hỗ trợ ba tầng logic:

| Tầng | Project | Phụ thuộc compile |
|---|---|---|
| Presentation | BookSales.Web | Application, Infrastructure |
| Business | BookSales.Application | Domain |
| Business | BookSales.Domain | Không phụ thuộc project khác |
| Data/Infrastructure | BookSales.Infrastructure | Application; EF Core SQL Server 8 |

Web là composition root đăng ký DI. SignalR adapter ở Web vì phụ thuộc hub do Web host. Infrastructure không gọi controller hoặc biết hub cụ thể; worker chỉ dùng `INoticePublisher`. Endpoint kiểm tra DB trong Web là probe kỹ thuật, chưa phải ví dụ controller nghiệp vụ truy cập dữ liệu trực tiếp.

Domain hiện rỗng: chưa tự dựng Book/Order/Count/Account dựa vào schema cũ. Infrastructure có `BookSalesDbContext` chỉ để kiểm tra cấu hình/kết nối, không mapping entity, không migration hay tự cập nhật database. Application hiện có service kỹ thuật, chưa có service xử lý UC nghiệp vụ.

## Đường đi của bài thử

`Razor form → POST /development/notices → DevelopmentNoticeService → INoticeQueue → ChannelNoticeQueue → NoticeDispatchWorker → INoticePublisher → SignalRNoticePublisher → DevelopmentNoticeHub → các tab đang kết nối`.

Request chờ validation và enqueue hoàn thành rồi trả HTTP 202. Worker tiêu thụ độc lập với request; việc nhận SignalR có thể xảy ra trước khi trình duyệt xử lý xong HTTP response. Không suy ra thứ tự tuyệt đối giữa hai màn hình UI.

| Thành phần | Trách nhiệm, quan hệ cần giải thích |
|---|---|
| DevelopmentController | Nhận request, kiểm tra input/anti-forgery và điều kiện local; dùng application service |
| DevelopmentNoticeService | Tạo notice, dùng queue abstraction qua constructor; giới hạn 200 ký tự là quy ước probe kỹ thuật |
| IntegrationNotice | Immutable record làm thông điệp kỹ thuật; không phải entity/table hay domain event nghiệp vụ |
| INoticeQueue / ChannelNoticeQueue | Interface và implementation; class đóng gói Channel, không lộ writer/reader ra controller |
| NoticeDispatchWorker | Kế thừa BackgroundService của framework; phụ thuộc queue và publisher abstractions |
| INoticePublisher / SignalRNoticePublisher | Tách điều phối nền khỏi công nghệ SignalR; dùng IHubContext để push |
| DevelopmentNoticeHub | Điểm kết nối client, kiểm tra Development/loopback |

OOP ở đây thể hiện qua trách nhiệm class, encapsulation, interface/implementation và constructor injection. Đây là ví dụ hạ tầng; phần OOP nghiệp vụ của mỗi thành viên vẫn phải được xây dựng theo luồng riêng. Không cần ép mỗi class có interface hoặc tạo kế thừa nghiệp vụ vô lý.

`await` trong request là async I/O, vẫn nằm trong vòng đời request. Queue + worker là xử lý nền tách khỏi request. SignalR là push tới client đang kết nối, không phải polling. Chạy hai tab và so sánh ID là bài chứng minh realtime ban đầu.

## Giới hạn cần nói đúng khi bảo vệ

- Queue bounded 128, `Wait` khi đầy; request cancellation được truyền xuống writer. Một reader xử lý tuần tự, nhiều request có thể enqueue.
- Queue trong RAM; dừng process có thể mất thông báo. Client offline/reconnect không được phát lại thông báo đã bỏ lỡ.
- Worker log lỗi publish từng thông báo và tiếp tục phần tử sau; chưa có retry, outbox, lưu bền hay client acknowledgment.
- Không dùng probe này để gửi giá/stock/sale thực. Chưa có transaction commit liên quan tới queue và chưa chốt tình huống giá đổi khi đang lập sale.
- Development routes/hub yêu cầu Development + loopback. Chưa triển khai Login, permission, access audit nghiệp vụ.
- `/health` không chứng nhận SQL khỏe. `/development/database` không chứng nhận schema/3NF hay dữ liệu đúng.

## Áp dụng cho năm thành viên

Bảng dưới nhắc lại **PROPOSAL — not yet approved** từ tài liệu phân công, không tạo assignment GitHub hoặc cam kết tất cả UC phải có code.

| Thành viên | Luồng chính đề xuất | Phần sẽ bổ sung sau khi chốt đặc tả |
|---|---|---|
| Nam | UC10 Update Price | Book behavior, application service, persistence và UI; xác nhận quyền, validation và thời điểm commit/phát sự kiện |
| Kiệt | UC07 Record Inventory Count | Count/history/stock và một Adjustment cho các Book lệch; xác nhận identity, stock representation, ý nghĩa dòng Book lặp |
| Hưng | UC01 Process Sale bằng Cash | Order/dòng bán, kiểm tra tồn và kết quả transaction; chốt scenario, không tự áp dụng price snapshot cũ |
| Hùng | UC12 Update Staff Permissions; Login hỗ trợ | Reconcile Employee/Account/Role theo ghi chú 22/10; thực thi quyền phía server và access log |
| Tech Lead | UC15 báo cáo tồn kho, tích hợp | Chốt cột/nguồn số liệu/export; nối các luồng có hỗ trợ yêu cầu vào async/realtime |

Mỗi luồng cần mô tả entity/behavior, abstraction, class relationships, Class Diagram và Sequence Diagram khớp code; chuẩn bị giải thích và viết lại đoạn cốt lõi. PlantUML là source UML mặc định của workspace. Không dùng tên bảng để suy ra class hay tạo sequence nhằm lấp yêu cầu thiếu.

Hiện chưa bổ sung diagram nghiệp vụ hay package repository generic. Những thay đổi tiếp theo cần dựa vào source/context chuẩn và scenario được thống nhất. Bài thử kỹ thuật không hoàn tất UC10 hoặc yêu cầu realtime nghiệp vụ.

## Chạy, kiểm tra và cấu hình

Xem README ở repository root. SDK được giới hạn ở 8.0.x bằng global.json; TargetFramework net8.0. EF Core SQL Server và MVC Testing 8.0.32; SignalR browser client 8.0.29 được lưu local cùng license, không cần CDN.

Tests bao gồm queue FIFO/backpressure/cancellation, worker tiếp tục sau một lần lỗi và HTTP production không mở endpoint dev. Kiểm tra trình duyệt hai tab bổ sung cho unit/integration tests. SQL probe mặc định báo chưa cấu hình; chưa kết nối DB draft cũ. Không gọi kết quả này là CI hoặc System Testing hoàn tất.
