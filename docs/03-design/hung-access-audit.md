# SWD392 — Hùng: Access & Audit / UC12

**Bản bàn giao cập nhật: 08/11/2026.** Tài liệu này thay cho bản cũ vẫn mô tả `UnconfiguredPasswordHashVerifier`. Chỉ mô tả code phần Hùng; UC16–UC20 là phạm vi modeling/review trong tài liệu phân công, **không được đánh dấu là đã code tất cả**.

## 1. Trạng thái và phạm vi

| Thành phần | Bằng chứng/trạng thái |
|---|---|
| Login ASP.NET Core Cookie, password hash | Đã đăng nhập bằng account demo thực tế sau khi thay `IdentityPasswordHashVerifier`; chưa có output test tự động mới nhất riêng cho Login. |
| `SqlPermissionChecker` / `AccessGuard` | Đã test truy vấn SQL 8/8; guard có unit test allow, deny, missing account, audit failure. |
| Access Log (`UserAccessLog`) | Đã kiểm chứng ghi SQL và rollback; các trang log được mở và thử trên trình duyệt. |
| UC12 Admin cấp/thu hồi | 6/6 kiểm thử tích hợp SQL có rollback; người thực hiện xác nhận demo trên trình duyệt. |
| UC12 Store Manager | Người thực hiện xác nhận đã thử sau khi cấu hình danh sách demo; cấu hình là **phương án nội bộ**, không phải chính sách chính thức đã được trưởng nhóm duyệt. |
| Unit tests | Kết quả terminal gần nhất có bằng chứng cụ thể: **23/23 Passed**; 3 bài test PasswordHasher được hướng dẫn sau đó và người thực hiện nói đã ổn, nhưng chưa có log kiểm chứng 26/26 gửi lại. |
| Tích hợp bảo vệ route của Nam/Kiệt/Hưng/Nghĩa | **Chưa có bằng chứng** các thành viên khác đã gắn `IAccessGuard` vào controller của họ. Đây là việc tích hợp chung còn mở. |
| Commit/push | Tại lần kiểm tra gần nhất branch `feature/hung-access-audit` đã đồng bộ remote trước batch cấu hình Manager. **Cần chạy lại `git status -sb` để xác nhận bản mới đã push.** |

### Quy tắc đã thực hiện

- `SYSTEM_ADMIN` có thể quản lý permission hợp lệ; website kiểm tra yêu cầu UC12 trước khi xử lý POST.
- `STORE_MANAGER` không được tự sửa quyền hoặc sửa quyền của `SYSTEM_ADMIN`, và phải có UC12. Mọi thay đổi Manager còn phải vượt qua **hai allowlist cấu hình**.
- Checker từ chối account vắng mặt, không `ACTIVE`, permission không tồn tại; quyền được đọc lại từ database mỗi lần `HasPermissionAsync`, không nhúng quyền cố định trong cookie.
- Login dùng `PasswordHasher<object>` qua interface `IPasswordHashVerifier`, cookie chỉ mang identity (`NameIdentifier`/`Name`); `OnValidatePrincipal` kiểm tra tài khoản còn `ACTIVE`.

### Phạm vi Manager đang dùng để demo (đề xuất, chưa chốt chính thức)

- Quyền: `DEMO-20261027-UC01`, `UC02`, `UC06`, `UC07`, `UC08`, `UC21` (mã đầy đủ có prefix `DEMO-20261027-`).
- Nhóm đích: `SALES_STAFF`, `INVENTORY_STAFF`.
- Hai danh sách là **allowlist độc lập**, nên theo code hiện tại Manager có thể gán bất kỳ quyền nào trong danh sách cho **cả hai** role đích; chưa có quy tắc ghép từng permission với role riêng.
- Cấu hình trong `appsettings.json` (dùng chung) và `appsettings.Local.json` (máy cá nhân); file Local có secret DB và không được commit.

## 2. Các lớp và code để chỉ khi thuyết trình

| Chức năng | Source file | Method/đoạn code thực tế |
|---|---|---|
| Kiểm tra quyền | `src/BookSales.Infrastructure/Access/SqlPermissionChecker.cs` | `HasPermissionAsync(accountId, permissionId, ct)`; LINQ `SingleOrDefaultAsync` JOIN Account–Employee; `AnyAsync` Permission/EmployeePermission. |
| Guard dùng chung | `src/BookSales.Infrastructure/Access/AccessGuard.cs` | `AuthorizeAsync(accountId, permissionId, requestPath, clientIp, ct)` gọi checker, writer rồi trả `bool`. |
| Ghi audit | `src/BookSales.Infrastructure/Access/SqlAccessLogWriter.cs` | `WriteAsync(...)` tạo `UserAccessLog`, `SaveChangesAsync()`. |
| UC12 điều phối | `src/BookSales.Application/Access/StaffPermissionService.cs` | `ChangeAsync(change, ct)` gọi policy trước, store sau. |
| UC12 chính sách | `src/BookSales.Infrastructure/Access/SqlStaffPermissionChangePolicy.cs` | `IsAllowedAsync(change, ct)` kiểm tra role, target, scope và UC12. |
| UC12 lưu quyền | `src/BookSales.Infrastructure/Access/SqlStaffPermissionStore.cs` | `ApplyAsync(employeeId, permissionId, grant, ct)` dùng `FindAsync`, `Add/Remove`, `SaveChangesAsync`. |
| UC12 HTTP | `src/BookSales.Web/Controllers/AccessController.cs` | `Permissions(GET/POST)`, `Demo(GET)`, `Logs(GET)`. POST UC12 gọi **checker trực tiếp** để kiểm UC12, không gọi AccessGuard, và ghi audit cùng DB transaction. |
| Login | `src/BookSales.Infrastructure/Access/SqlAccountLoginService.cs` | `VerifyAsync(username, password, ct)` lọc Account `ACTIVE` rồi gọi `IPasswordHashVerifier.Verify()`. |
| Xác minh hash | `src/BookSales.Web/Access/IdentityPasswordHashVerifier.cs` | `Verify(storedHash, providedPassword)` dùng ASP.NET Core Identity PasswordHasher. |
| Cookie | `src/BookSales.Web/Controllers/AuthController.cs`, `src/BookSales.Web/Program.cs` | `Login(GET/POST)`, `Logout(POST)`, `SignInAsync`, `OnValidatePrincipal`. |
| Audit request ẩn danh | `src/BookSales.Web/Middleware/AccessAuditMiddleware.cs` | `InvokeAsync(context, writer)` chỉ áp dụng `/access/permissions`, `/access/demo`, `/access/logs`, `/access/logout`. |

**Không có** method riêng `FindAccountByAccountId()`. `Account JOIN Employee` là truy vấn LINQ trong `SqlPermissionChecker.HasPermissionAsync()`, kết thúc bằng `SingleOrDefaultAsync()`. Sequence Diagram không được ghi đó là lời gọi method tự tạo.

## 3. Luồng mẫu đã thực hiện

### A. Protected demo (`GET /access/demo`)

1. Cookie auth xác minh identity; `[Authorize]` yêu cầu đăng nhập.
2. `AccessController.Demo()` gọi `CheckAsync(...)` → `IAccessGuard.AuthorizeAsync(...)` với `accountId` lấy từ **claim server**, permission `UC21` lấy từ cấu hình.
3. `AccessGuard` gọi `IPermissionChecker.HasPermissionAsync()` → truy vấn Account + Employee, sau đó `Permissions.AnyAsync()` hoặc `EmployeePermissions.AnyAsync()` theo role.
4. Guard gọi `IAccessLogWriter.WriteAsync()` với kết quả allow/deny, rồi trả bool.
5. Controller trả `Content(...)` nếu allow hoặc `Forbid()` nếu deny.

### B. UC12 (`POST /access/permissions`)

1. `[Authorize]` + anti-forgery. Controller gọi checker kiểm UC12 trước; thiếu quyền thì ghi denied log và `Forbid()`.
2. Nếu model hợp lệ: `db.Database.BeginTransactionAsync()`.
3. `StaffPermissionService.ChangeAsync()` → `SqlStaffPermissionChangePolicy.IsAllowedAsync()` → nếu allow thì `SqlStaffPermissionStore.ApplyAsync()`.
4. Controller thêm `UserAccessLog` với `WasAuthorized = changed`, `SaveChangesAsync()` và `CommitAsync()` trong **cùng transaction**. Nếu policy từ chối, không đổi quyền nhưng vẫn lưu denial audit.
5. `RedirectToAction(nameof(Permissions))`; request truy cập tiếp theo đọc lại quyền hiện tại trong SQL.

### C. Access Log

- `account_id` (nullable), `occurred_at_utc`, `request_path`, `was_authorized`, `client_ip`.
- `was_authorized` trong guard nghĩa là **quyết định quyền truy cập**, không khẳng định mọi thao tác nghiệp vụ sau đó thành công. Với UC12 POST, audit ghi kết quả chính sách cập nhật (`changed`).
- `GET /access/logs`: Admin thấy tất cả, Manager lọc không thấy log của SYSTEM_ADMIN hoặc anonymous theo code đang có, tối đa 100 dòng gần nhất.
- Middleware hiện chỉ ghi lượt **anonymous tại các route `/access/*` cụ thể**. Module khác phải tự dùng `IAccessGuard` nếu muốn ghi kết quả authenticated allow/deny; không nên tuyên bố middleware tự audit toàn bộ website.

## 4. Kiểm thử/bảo vệ

- Demo: Admin Login → cấp UC21 cho Sales B → đăng nhập Sales B và thử `/access/demo` (allow) → Admin thu hồi → Sales B thử lại (deny) → Admin/Manager xem `/access/logs`.
- Tình huống sai: Manager tự sửa quyền → deny; tài khoản `DISABLED` không được đăng nhập/checker; unconfigured/missing permission → deny.
- Giải thích OOP: Controller → interface/service → policy/store → EF Core; `async/await` cho I/O; transaction đảm bảo UC12 không đổi quyền khi phần lưu audit thất bại.
- Khi trình bày “realtime”, không tuyên bố Access module tự dùng SignalR. Phần Hùng chỉ bảo đảm **hiệu lực quyền ở request tiếp theo**. Realtime notification thuộc phần tích hợp chung.

## 5. File bàn giao và việc còn mở

1. `docs/03-design/hung-access-diagrams.drawio`: 4 tab UML, method names đối chiếu source, có nhánh UC12 denied và SQL queries phân biệt với method thực.
2. `docs/03-design/hung-access-integration.md`: cách các module khác gắn guard, log và những lưu ý về authentication.
3. `docs/ai-usage/hung-access-audit-assistance.md`: đã cập nhật thông tin hỗ trợ AI cho bản tài liệu và diagram mới.
4. Trước merge: xem Git diff, test lại, check secret, tạo **Draft Pull Request** tới main để nhóm review. Không tự merge.
5. **Cần phối hợp** Nam, Kiệt, Hưng, Nghĩa gắn checker/guard vào endpoint thực tế và xác nhận quyền tương ứng; phần này ngoài scope thực thi cá nhân đã kiểm chứng.
