using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data.Entities;

namespace TradingSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<FeatureFlagEntity> FeatureFlags => Set<FeatureFlagEntity>();
    public DbSet<OtpEntity> Otps => Set<OtpEntity>();
    public DbSet<ActivityLogEntity> ActivityLogs => Set<ActivityLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Plan).HasColumnName("plan").IsRequired().HasMaxLength(20).HasDefaultValue("Basic");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<FeatureFlagEntity>(entity =>
        {
            entity.ToTable("feature_flags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FeatureKey).HasColumnName("feature_key").IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(50);
            entity.HasIndex(e => e.FeatureKey).IsUnique();
        });


        modelBuilder.Entity<OtpEntity>(entity =>
        {
            entity.ToTable("otps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).HasColumnName("code").IsRequired().HasMaxLength(6);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => new { e.Email, e.Code }).IsUnique();
        });

        modelBuilder.Entity<ActivityLogEntity>(entity =>
        {
            entity.ToTable("activity_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Region).HasColumnName("region").HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(5);
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Isp).HasColumnName("isp").HasMaxLength(200);
            entity.Property(e => e.Timezone).HasColumnName("timezone").HasMaxLength(50);
            entity.Property(e => e.DeviceType).HasColumnName("device_type").HasMaxLength(20);
            entity.Property(e => e.Browser).HasColumnName("browser").HasMaxLength(50);
            entity.Property(e => e.OperatingSystem).HasColumnName("os").HasMaxLength(50);
            entity.Property(e => e.IsSuccess).HasColumnName("is_success").HasDefaultValue(true);
            entity.Property(e => e.Details).HasColumnName("details").HasMaxLength(500);
            entity.Property(e => e.HttpMethod).HasColumnName("http_method").HasMaxLength(10);
            entity.Property(e => e.RequestPath).HasColumnName("request_path").HasMaxLength(200);
            entity.Property(e => e.SessionId).HasColumnName("session_id").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Indexes for fast querying
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Username);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.CountryCode);
        });
    }
}


