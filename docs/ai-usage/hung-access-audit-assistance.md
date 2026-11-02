# SWD392 — Access & Audit implementation assistance

Các phần hỗ trợ lập trình: model EF Core 5 bảng, cookie-authentication foundation, dịch vụ kiểm quyền mỗi request, audit writer, AccessGuard, UC12 policy/store/service, website login/permissions/demo/logs, unit tests, integration probe rollback và Class/Sequence diagrams.

Người triển khai phải chạy lại build và test tại môi trường .NET 8/SQL Server, đối chiếu quy tắc nghiệp vụ do nhóm chốt, kiểm tra diff trước khi commit và không chứa connection string/mật khẩu trong commit. Hash format và Manager editing scopes vẫn cố ý chưa được triển khai khi thiếu phê duyệt.
