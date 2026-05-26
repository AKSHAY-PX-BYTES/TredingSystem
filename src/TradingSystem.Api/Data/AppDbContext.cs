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
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<PriceAlertEntity> PriceAlerts => Set<PriceAlertEntity>();
    public DbSet<AiSignalEntity> AiSignals => Set<AiSignalEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<FeedbackEntity> Feedbacks => Set<FeedbackEntity>();

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
            entity.Property(e => e.Plan).HasColumnName("plan").IsRequired().HasMaxLength(20).HasDefaultValue("Free");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(5);
            entity.Property(e => e.IsPhoneVerified).HasColumnName("is_phone_verified").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.TrialEndsAt).HasColumnName("trial_ends_at");
            entity.Property(e => e.IsTrialUsed).HasColumnName("is_trial_used").HasDefaultValue(false);
            entity.Property(e => e.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(10).HasDefaultValue("en");
            entity.Property(e => e.PreferredCurrency).HasColumnName("preferred_currency").HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
            entity.Property(e => e.LockoutEndUtc).HasColumnName("lockout_end_utc");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").HasMaxLength(200);
            entity.Property(e => e.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
            
            // Profile fields
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.TradingExperience).HasColumnName("trading_experience").HasMaxLength(20);
            
            // Legal consents
            entity.Property(e => e.ConsentFinancialRisk).HasColumnName("consent_financial_risk").HasDefaultValue(false);
            entity.Property(e => e.ConsentTermsAndConditions).HasColumnName("consent_terms").HasDefaultValue(false);
            entity.Property(e => e.ConsentPrivacyPolicy).HasColumnName("consent_privacy").HasDefaultValue(false);
            entity.Property(e => e.ConsentAiSignals).HasColumnName("consent_ai_signals").HasDefaultValue(false);
            entity.Property(e => e.ConsentedAt).HasColumnName("consented_at");
            
            // Security
            entity.Property(e => e.PasswordChangedAt).HasColumnName("password_changed_at");
            entity.Property(e => e.RecoveryEmail).HasColumnName("recovery_email").HasMaxLength(100);
            entity.Property(e => e.MfaEnabled).HasColumnName("mfa_enabled").HasDefaultValue(false);
            entity.Property(e => e.MfaSecret).HasColumnName("mfa_secret").HasMaxLength(200);
            entity.Property(e => e.SessionToken).HasColumnName("session_token").HasMaxLength(200);
            entity.Property(e => e.SessionTokenIssuedAt).HasColumnName("session_token_issued_at");
            
            // Preferences
            entity.Property(e => e.Theme).HasColumnName("theme").HasMaxLength(10).HasDefaultValue("system");
            entity.Property(e => e.NotifyWhatsNew).HasColumnName("notify_whats_new").HasDefaultValue(true);
            entity.Property(e => e.NotifyRecommendations).HasColumnName("notify_recommendations").HasDefaultValue(true);
            entity.Property(e => e.NotifyEmailUpdates).HasColumnName("notify_email_updates").HasDefaultValue(true);
            
            // Account deletion
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletionReason).HasColumnName("deletion_reason").HasMaxLength(500);

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

        modelBuilder.Entity<SubscriptionEntity>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Plan).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PricePerMonth).HasColumnType("decimal(10,2)");
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Symbol).HasMaxLength(20);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<PriceAlertEntity>(entity =>
        {
            entity.ToTable("price_alerts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TargetPrice).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ThresholdPercent).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });

        modelBuilder.Entity<AiSignalEntity>(entity =>
        {
            entity.ToTable("ai_signals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SignalType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Confidence).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.GeneratedAt);
        });

        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.SessionId });
        });

        modelBuilder.Entity<PasswordResetTokenEntity>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Token).HasColumnName("token").IsRequired().HasMaxLength(200);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired(false);
            entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired().HasMaxLength(100);
            entity.Property(e => e.PaymentId).HasColumnName("payment_id").HasMaxLength(100);
            entity.Property(e => e.Plan).HasColumnName("plan").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            entity.Property(e => e.Signature).HasColumnName("signature").HasMaxLength(500);
            entity.Property(e => e.IsAnnual).HasColumnName("is_annual").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.RawResponse).HasColumnName("raw_response");
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<FeedbackEntity>(entity =>
        {
            entity.ToTable("feedbacks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Type).HasColumnName("type").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Subject).HasColumnName("subject").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).HasColumnName("message").IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20).HasDefaultValue("Open");
            entity.Property(e => e.AdminResponse).HasColumnName("admin_response").HasMaxLength(5000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}


