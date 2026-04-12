using Microsoft.EntityFrameworkCore;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;

namespace ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;

public sealed class LoginDbContext(DbContextOptions<LoginDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.UseCollation("utf8mb4_unicode_ci");

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Username).HasMaxLength(64).IsRequired();
            builder.Property(entity => entity.DisplayName).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.Mobile).HasMaxLength(32);
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.Property(entity => entity.UpdatedAtUtc).HasPrecision(6);
            builder.Property(entity => entity.LockoutEndUtc).HasPrecision(6);
            builder.HasIndex(entity => entity.Username).IsUnique();
            builder.HasIndex(entity => entity.Mobile).IsUnique();
        });

        modelBuilder.Entity<UserCredential>(builder =>
        {
            builder.ToTable("user_credentials");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Provider).IsRequired();
            builder.Property(entity => entity.Subject).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.SecretHash).HasMaxLength(512).IsRequired();
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.Property(entity => entity.UpdatedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => new { entity.Provider, entity.Subject }).IsUnique();
            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.Credentials)
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("roles");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
            builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.Property(entity => entity.UpdatedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("permissions");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Code).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => entity.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(builder =>
        {
            builder.ToTable("user_roles");
            builder.HasKey(entity => new { entity.UserId, entity.RoleId });
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.UserRoles)
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.Role)
                .WithMany(entity => entity.UserRoles)
                .HasForeignKey(entity => entity.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("role_permissions");
            builder.HasKey(entity => new { entity.RoleId, entity.PermissionId });
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.HasOne(entity => entity.Role)
                .WithMany(entity => entity.RolePermissions)
                .HasForeignKey(entity => entity.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.Permission)
                .WithMany(entity => entity.RolePermissions)
                .HasForeignKey(entity => entity.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.CreatedByIp).HasMaxLength(64);
            builder.Property(entity => entity.CreatedByUserAgent).HasMaxLength(256);
            builder.Property(entity => entity.RevokedReason).HasMaxLength(128);
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.Property(entity => entity.ExpiresAtUtc).HasPrecision(6);
            builder.Property(entity => entity.RevokedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => entity.TokenHash).IsUnique();
            builder.HasOne(entity => entity.User)
                .WithMany(entity => entity.RefreshTokens)
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoginAttempt>(builder =>
        {
            builder.ToTable("login_attempts");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Identity).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.FailureReason).HasMaxLength(128);
            builder.Property(entity => entity.IpAddress).HasMaxLength(64);
            builder.Property(entity => entity.UserAgent).HasMaxLength(256);
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => new { entity.Identity, entity.CreatedAtUtc });
            builder.HasOne(entity => entity.User)
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Action).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.Detail).HasMaxLength(512).IsRequired();
            builder.Property(entity => entity.IpAddress).HasMaxLength(64);
            builder.Property(entity => entity.UserAgent).HasMaxLength(256);
            builder.Property(entity => entity.CreatedAtUtc).HasPrecision(6);
            builder.HasIndex(entity => new { entity.UserId, entity.CreatedAtUtc });
            builder.HasOne(entity => entity.User)
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}