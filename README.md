# BookSales · SWD392 Group 1

Khung phát triển cho hệ thống quản lý bán sách nội bộ, dùng **.NET 8**, ASP.NET Core MVC, EF Core/SQL Server và SignalR. Người dùng xác nhận hướng công nghệ này ngày 27/10/2026; phiên bản .NET được chọn rõ là 8.

Hiện có nền tảng chạy được và bài thử async/realtime. Các luồng nghiệp vụ của năm thành viên chưa triển khai. Việc dựng khung không xác nhận SRS, ERD/3NF hay Baseline đã được duyệt.

## Chạy nhanh

Cài **.NET SDK 8.0.x** (chỉ có runtime hoặc SDK 10 là chưa đủ). Mở PowerShell tại thư mục repository:

```powershell
.\scripts\dev.ps1 restore
.\scripts\dev.ps1 build
.\scripts\dev.ps1 test
.\scripts\dev.ps1 run
```

Mở [trang tổng quan](http://127.0.0.1:5180/) hoặc [bài thử realtime](http://127.0.0.1:5180/development). Dừng server bằng Ctrl+C. `global.json` chỉ chọn SDK 8; script dùng SDK 8 đã cài trên máy hoặc bản riêng tại `$env:USERPROFILE\.cache\swd392-dotnet8` nếu có. Script không tự tải/cài SDK.

Nếu SDK 8 đã cài chuẩn, có thể dùng trực tiếp:

```powershell
dotnet restore BookSales.sln
dotnet build BookSales.sln
dotnet test BookSales.sln
dotnet run --project src/BookSales.Web --launch-profile http
```

Không cần Node.js hay SQL Server để chạy bài thử. Trong Visual Studio hỗ trợ .NET 8, mở `BookSales.sln`, chọn `BookSales.Web` và profile `http`.

## Thử async/realtime

1. Mở `/development` ở hai tab và đợi cả hai báo **Đã kết nối**.
2. Gửi nội dung từ một tab. POST trả `202` khi queue chấp nhận.
3. Worker đọc queue rồi phát SignalR; cả hai tab hiện cùng nội dung và ID mà không refresh.

Queue dùng bộ nhớ, tối đa 128 phần tử và chờ khi đầy. Không lưu bền, không retry hoặc phát lại cho client offline. `202` không phải xác nhận client đã nhận. Trang, API và hub thử nghiệm chỉ mở trong Development từ địa chỉ loopback. Đây chưa phải Login/phân quyền nghiệp vụ và chưa phải UC10 đổi giá.

## Cấu trúc và ranh giới

| Đường dẫn | Trách nhiệm hiện có |
|---|---|
| `src/BookSales.Web` | MVC, Razor, endpoint thử nghiệm, SignalR hub/publisher, cấu hình DI |
| `src/BookSales.Application` | Điều phối bài thử; abstraction queue/publisher, DTO thông báo |
| `src/BookSales.Domain` | Vị trí cho entity và hành vi nghiệp vụ sau khi chốt đặc tả; hiện chưa có entity |
| `src/BookSales.Infrastructure` | Channel, BackgroundService và DbContext kết nối SQL Server chưa có mapping |
| `tests/BookSales.Tests` | Queue/backpressure/cancellation, worker và giới hạn endpoint Production |
| `docs/01-requirements`, `docs/02-analysis` | Giữ cấu trúc dành cho artifact yêu cầu và phân tích |
| `docs/03-design` | [Hướng dẫn khung .NET 8](docs/03-design/dotnet-foundation.md) |
| `docs/ai-usage` | [Ghi nhận AI và kiểm tra local](docs/ai-usage/dotnet-foundation-2026-10-27.md) |

Ba tầng logic là Presentation, Business và Data/Infrastructure. Application + Domain thuộc tầng Business; bốn project không phải bốn microservice. Mỗi module sẽ có luồng xuyên tầng của mình, không chia thành người chỉ code UI hay chỉ code database.

## SQL Server tùy chọn

Mặc định `ConnectionStrings:BookSales` trống. `/health` chỉ báo process chạy và có cấu hình DB hay chưa, không kiểm tra SQL Server. Endpoint `/development/database` thực hiện kiểm tra kết nối khi đã cấu hình, không tạo hoặc sửa schema.

Có thể cấu hình bằng biến môi trường trước khi chạy:

```powershell
$env:ConnectionStrings__BookSales = 'Server=YOUR_SERVER;Database=YOUR_REVIEWED_DATABASE;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
.\scripts\dev.ps1 run
```

Tên server/database là giá trị thay thế. Dùng SQL Server có cấu hình chứng chỉ phù hợp với chuỗi kết nối của máy bạn. Hoặc tạo `src/BookSales.Web/appsettings.Local.json` (đã gitignore) với cấu trúc:

```json
{
  "ConnectionStrings": {
    "BookSales": "YOUR_LOCAL_CONNECTION_STRING"
  }
}
```

Biến môi trường và tham số dòng lệnh được ưu tiên hơn file local. Không commit mật khẩu hay file cấu hình cá nhân. Khung này không chạy `EnsureCreated`/`Migrate`, chưa có DbSet/migration và chưa gắn với DB DEV_DRAFT trước đây. Schema và luồng Count/Return/Permissions còn cần reconciliation trước khi triển khai.

## Làm việc trong nhóm

1. Tạo task issue có mục tiêu, COMET phase, deliverables và acceptance criteria.
2. Phân công người làm, ghi dependency; làm trên nhánh riêng.
3. Mở pull request vào `main` theo template `.github/pull_request_template.md`.
4. Nhờ thành viên khác review trước khi merge.

Chỉ dẫn tới issue, yêu cầu, UC, diagram và test thực sự tồn tại. Không tự đặt yêu cầu, trạng thái hoặc ID để điền template. Khung được tạo theo yêu cầu trực tiếp của Tech Lead, trên nhánh `codex/dotnet-foundation`. Kiểm tra lịch sử Git/GitHub để xác định trạng thái remote hiện tại. Tham khảo context chuẩn trong workspace cha tại `Project Context/SWD392_PROJECT_CONTEXT.md` khi có; tài liệu này không thay thế context chuẩn.

Trình tự trình bày của giảng viên vẫn là Context Diagram → Use Case → ERD → 3NF/Database Design → Baseline → Build project. Requirements, Analysis, Design, Construction, Integration và System Testing vẫn là các phạm vi khác nhau trong COMET. Baseline còn `OPEN DECISION`.

## Kiểm chứng và AI

Ngày 27/10/2026: build Debug thành công, test Debug và Release đều 7/7 qua bằng SDK 8.0.427; đã thử hai tab nhận cùng thông báo SignalR và kiểm tra đầu vào/CSRF qua HTTP local. Đây là kiểm tra local, chưa có CI/deploy hoặc nghiệm thu nghiệp vụ. Xem [bản ghi chi tiết](docs/ai-usage/dotnet-foundation-2026-10-27.md).

AI hỗ trợ tạo khung và tài liệu. Thành viên phải đọc, chạy, giải thích và review trước khi nhận code là kết quả của nhóm. Thư viện SignalR browser dùng MIT; xem [ghi nhận thư viện](THIRD_PARTY_NOTICES.md).
