using BookSalesManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesManagement.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Account", "dbo");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).HasColumnName("account_id").HasMaxLength(200);
        builder.Property(account => account.EmployeeId).HasColumnName("employee_id").HasMaxLength(200);
        builder.Property(account => account.Username).HasColumnName("username").HasMaxLength(200);
        builder.Property(account => account.Status).HasColumnName("status").HasColumnType("nvarchar(max)");
        builder.Ignore(account => account.IsActive);
    }
}

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee", "dbo");
        builder.HasKey(employee => employee.Id);
        builder.Property(employee => employee.Id).HasColumnName("employee_id").HasMaxLength(200);
        builder.Property(employee => employee.Name).HasColumnName("name").HasColumnType("nvarchar(max)");
        builder.Property(employee => employee.RoleCode).HasColumnName("role_code").HasMaxLength(32);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission", "dbo");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id).HasColumnName("permission_id").HasMaxLength(200);
        builder.Property(permission => permission.Name).HasColumnName("permission_name").HasColumnType("nvarchar(max)");
    }
}

public sealed class EmployeePermissionConfiguration : IEntityTypeConfiguration<EmployeePermission>
{
    public void Configure(EntityTypeBuilder<EmployeePermission> builder)
    {
        builder.ToTable("EmployeePermission", "dbo");
        builder.HasKey(link => new { link.EmployeeId, link.PermissionId });
        builder.Property(link => link.EmployeeId).HasColumnName("employee_id").HasMaxLength(200);
        builder.Property(link => link.PermissionId).HasColumnName("permission_id").HasMaxLength(200);
    }
}
