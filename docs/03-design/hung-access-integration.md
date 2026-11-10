# Hùng → Team: Cách tích hợp Access & Audit (SWD392)

**Mục đích:** Các module nghiệp vụ khác dùng Login Cookie, Permission Checker và Access Log chung mà **không copy/paste lại SQL truy vấn quyền**.

## Dịch vụ đã đăng ký DI trong `src/BookSales.Web/Program.cs`

- `IAccessGuard` → `AccessGuard`: check permission + write UserAccessLog, trả `bool`.
- `IPermissionChecker` → `SqlPermissionChecker`: check quyền hiện tại từ SQL **mỗi lần gọi** (không tự ghi log).
- `IAccessLogWriter` → `SqlAccessLogWriter`: lưu log, không tự quyết định quyền.
- Cookie authentication theo `CookieAuthenticationDefaults.AuthenticationScheme`; login được làm sẵn ở `/access/login`.

## Mẫu gắn vào Controller của thành viên khác

**Ví dụ giả định** cho UC01 Process Sale. Đây là đoạn **mẫu tích hợp**, không có nghĩa team đã code hoặc kiểm thử controller `SalesController` như bên dưới.

```csharp
using System.Security.Claims;
using BookSales.Application.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class SalesController(IAccessGuard accessGuard) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        // Lấy AccountId từ cookie đã được server xác minh, không lấy từ form.
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // PermissionId thuộc bộ demo hiện tại của database.
        var allowed = await accessGuard.AuthorizeAsync(
            accountId,
            "DEMO-20261027-UC01",
            Request.Path.Value ?? "/sales/complete",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            cancellationToken);

        if (!allowed) return Forbid();

        // Chỉ đặt thao tác bán hàng thật ở SAU check; đoạn này là placeholder,
        // không phải implementation UC01 của Hưng.
        return Ok("Authorized for UC01; business flow not shown here.");
    }
}
```

### Cách đọc mẫu

1. `[Authorize]`: request phải có phiên cookie hợp lệ; dùng thêm `[ValidateAntiForgeryToken]` cho POST.
2. `ClaimTypes.NameIdentifier`: dùng AccountId đã được server xác thực, **không nhận `accountId` từ client**.
3. `IAccessGuard.AuthorizeAsync(accountId, permissionId, path, ip, ct)`: lấy kết quả **true/false**; nội bộ guard gọi checker và writer. Nếu log thất bại guard ném lỗi, không trả allow.
4. `if (!allowed) return Forbid();`: kiểm tra ở server **trước** khi thay đổi dữ liệu nghiệp vụ.
5. Permission ID phải trùng **dbo.Permission.permission_id** trong DB của nhóm; tránh suy ra mã từ tên bằng cách đoán.

> Lưu ý: `AccessAuditMiddleware` hiện chỉ audit request **anonymous** tới `/access/permissions`, `/access/demo`, `/access/logs` và `/access/logout`; nó KHÔNG tự ghi anonymous denied cho `/sales/complete` hoặc route mới. Khi team cần log anonymous toàn website, phải thống nhất mở rộng middleware và kiểm thử. Khi đã authenticated, guard sẽ ghi allow/deny nếu action thực sự gọi guard.

## Những điểm không được tự làm sai

- Không dùng UI để quyết định cuối cùng; mỗi action cần kiểm tra quyền **server-side**.
- Không cache permission vào cookie. Cookie chỉ mang `NameIdentifier` và `Name`; checker truy vấn DB mới mỗi lần.
- `UserAccessLog.was_authorized` nói về **authorization**, không thay thế audit giao dịch nghiệp vụ.
- Không tự thêm các bảng Identity (`AspNetUsers`, `AspNetRoles`); dự án dùng bảng `Account` sẵn có và `PasswordHasher` để xác minh hash.
- Không commit `src/BookSales.Web/appsettings.Local.json`, connection string hoặc mật khẩu demo; không ghi secret vào log.
- UC12 dùng transaction chung cho thay đổi quyền và audit (xem `AccessController.Permissions(POST)`); các module khác cần quyết định atomicity riêng theo nghiệp vụ.

## Checklist để Nam, Kiệt, Hưng và Nghĩa tích hợp

- [ ] Xác định endpoint + permission ID thực sự cần bảo vệ (UC01/UC07/UC10/UC15 hoặc quyền khác theo phần được giao).
- [ ] Inject `IAccessGuard` vào controller/service boundary thích hợp.
- [ ] Lấy AccountId từ authenticated claim.
- [ ] Gọi `AuthorizeAsync` trước nghiệp vụ; nếu false thì trả `Forbid()`.
- [ ] Test 2 tài khoản có/không có quyền; test thu hồi quyền phản ánh ở request tiếp theo.
- [ ] Kiểm tra một bản ghi `UserAccessLog` allow và một bản ghi deny có path/IP.
- [ ] Cập nhật Sequence Diagram/method đúng với controller của chính thành viên đó.

## Tình trạng bàn giao

**Hùng đã triển khai nền tảng và demo UC12.** Việc Nam/Kiệt/Hưng/Nghĩa *đã sử dụng guard trong module của họ hay chưa* phải được xác nhận bằng code/PR riêng; tài liệu này là hướng dẫn tích hợp chứ không phải chứng cứ họ đã tích hợp.
