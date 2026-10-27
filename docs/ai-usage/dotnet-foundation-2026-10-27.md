# Ghi nhận AI — khung .NET 8, 27/10/2026

## Yêu cầu và phạm vi

Người dùng/Tech Lead yêu cầu tạo code project tại repository nhóm, xác nhận giữ C# ASP.NET Core MVC, ba tầng, SQL Server, SignalR và sau đó chỉ định **.NET 8**. Công việc được thực hiện bởi OpenAI Codex trong chat hiện tại. AI dùng .NET CLI, chỉnh source/tài liệu và kiểm tra bằng HTTP/browser; người dùng chưa nghiệm thu code.

COMET: Design/Construction/Integration cho hạ tầng kỹ thuật. Việc dựng khung không xác nhận các giai đoạn Requirements/Analysis/Database Design hoặc Baseline đã hoàn tất.

Repo clone: `swd392-group-1/swd392-sales-management-system`, base main `5f854db8290841fc469a64a68fb21b9d519df802`; nhánh `codex/dotnet-foundation`. Tại lần dựng khung đầu, chưa tạo issue, PR, commit hoặc push. Sau đó trong cùng ngày, người dùng yêu cầu push code lên GitHub; việc chuẩn bị commit/push chỉ bao gồm khung đã kiểm tra và tài liệu liên quan. Kiểm tra lịch sử Git/GitHub để xác định kết quả remote hiện tại. Không sửa bảng, dữ liệu, tracker hay assignment GitHub.

## Artifact do AI hỗ trợ

- Solution và bốn project Web/Application/Domain/Infrastructure target `net8.0`; project xUnit tests.
- MVC/Razor tiếng Việt: tổng quan năm module chưa triển khai và trang kiểm tra local.
- Queue bounded, worker nền và SignalR push; controller/service/contracts kỹ thuật.
- DbContext trống và connection configuration SQL Server tùy chọn; không migration/DbSet/seed.
- README, hướng dẫn thiết kế hạ tầng, script PowerShell, global.json/.gitignore và ghi nhận thư viện SignalR/MIT.

Nguồn ngữ nghĩa: context chuẩn `Project Context/SWD392_PROJECT_CONTEXT.md` ở workspace cha và các quyết định trực tiếp trong chat. Không đưa schema hoặc các giả định trước reset vào entity nghiệp vụ. Hướng dẫn kiểm tra luồng của skill `vercel:verification` được áp dụng cho local MVC; không có Vercel service/deployment được tạo. Browser dùng công cụ computer-use.

## Kiểm chứng local thực sự đã chạy

Máy Windows, PowerShell; .NET SDK **8.0.427** được cài riêng tại `C:\Users\PC\.cache\swd392-dotnet8` từ script chính thức `https://dot.net/v1/dotnet-install.ps1`. SDK 10 có sẵn không được dùng để xác nhận các kết quả bên dưới. SDK riêng không được đưa vào Git.

| Kiểm tra | Kết quả ngày 27/10/2026 |
|---|---|
| `dotnet build BookSales.sln --no-restore` bằng SDK 8 | Thành công; 0 warnings, 0 errors |
| `dotnet test BookSales.sln --no-build` Debug | 7 passed, 0 failed/skipped |
| `dotnet test BookSales.sln --configuration Release` | Restore/build và 7 tests thành công |
| Queue | Không thay notice đã nhận khi write bị hủy; writer chờ khi đầy; FIFO |
| Worker | Một publish lỗi không làm dừng notice tiếp theo |
| Production HTTP qua WebApplicationFactory | Home/health trả 200, DB chưa cấu hình; dev GET/POST và hub negotiate trả 404 |
| Browser local hai tab | Cả hai kết nối SignalR và nhận cùng nội dung/ID mà không refresh |
| HTTP Development local | Thiếu anti-forgery, message trắng hoặc dài 201 ký tự đều trả 400 |
| SQL probe chưa cấu hình | Trả `{"configured":false,"connected":false}`; không kết nối DB DEV_DRAFT |
| Console trình duyệt sau bài thử | Không có error/warning trong bản ghi thu được |

Thông báo thử: `Kiểm tra .NET 8: queue → worker → SignalR`, ID `3e1740eb-ce3e-4d38-b0db-c9cc45d556de`, hiển thị trên cả hai tab. Ảnh local `.artifacts/realtime-net8.jpg` được gitignore, không phải artifact CI. Terminal ghi worker dispatch cùng ID; dispatch thành công không phải bằng chứng client acknowledgment tổng quát.

Sau build cuối và khởi động lại bằng `scripts/dev.ps1 run`, đã reload hai tab và gửi `.NET 8 · Async / Realtime sẵn sàng`, ID `a9c09b29-b0b2-425c-8747-278939ee5384`. Cả hai tab lại nhận cùng ID; ảnh bàn giao tại đường dẫn trên được cập nhật theo lần thử này.

## Giới hạn và review của con người

Đây là nền tảng, chưa hoàn thành UC của cá nhân hoặc phần OOP nghiệp vụ. Không có Login, permission/audit nghiệp vụ, Book/Order/Count/Return, report export, QR, email hoặc vận chuyển. Chưa nghiệm thu scenario/diagram/schema; chưa live-test SQL Server; không có CI/deploy. Không tuyên bố thông báo lưu bền, replay/retry hoặc transaction/outbox.

Các test kiểm tra chính sách hạ tầng và endpoint; chưa thay thế integration/system tests của năm luồng nghiệp vụ. Nhóm cần review code và diagram theo đặc tả hiện hành, tự chạy trên máy thành viên, giải thích dependency/lifetime, queue cancellation, worker và SignalR trước khi nhận code để bảo vệ môn học. Human verification/acceptance: **chưa ghi nhận**.
