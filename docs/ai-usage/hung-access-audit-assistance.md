# SWD392 — Access & Audit implementation assistance

## Hỗ trợ code (tháng 11/2026)

AI hỗ trợ các phần: mô hình EF Core năm bảng, cookie authentication foundation, dịch vụ kiểm quyền mỗi request, audit writer, AccessGuard, UC12 policy/store/service, form Login/permissions/demo/logs, unit tests, integration probe SQL có rollback. Người thực hiện đã kiểm thử trên Windows / .NET 8 / SQL Server; các kết quả có log được nêu trong tài liệu `docs/03-design/hung-access-audit.md`.

Sau khi trưởng nhóm đồng ý dùng công cụ có sẵn của .NET, AI đề xuất `PasswordHasher<object>` qua `IPasswordHashVerifier`, không tạo mới bảng ASP.NET Identity; người thực hiện báo đã đăng nhập demo được. Phạm vi quyền Manager là cấu hình demo nội bộ và cần nhóm review trước khi triển khai chính thức.

## Hỗ trợ tài liệu/diagram (08/11/2026)

AI tạo bản cập nhật `hung-access-audit.md`, `hung-access-integration.md` và `hung-access-diagrams.drawio`; đối chiếu tên method và đường gọi đã được người thực hiện chia sẻ. Diagram thể hiện riêng LINQ/EF Core `SingleOrDefaultAsync` / `AnyAsync`, không gọi truy vấn này là method nghiệp vụ tự tạo. Các artefact này chưa được mở trên draw.io và review trực tiếp bởi người thực hiện tại thời điểm tạo; cần tự kiểm tra sơ đồ, liên kết, tên hàm với checkout mới nhất.

## Trách nhiệm kiểm chứng

Người thực hiện kiểm tra build, toàn bộ tests, traceability, Git diff, chống lộ secret, cùng các rule Manager / Admin trước khi commit/push và trước khi merge PR. Không commit `appsettings.Local.json` hoặc mật khẩu demo. Module khác chỉ được coi là đã tích hợp khi có code/test/PR tương ứng, không suy luận từ tài liệu mẫu.
