using Insurance_Hub.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DataProtection keys — persisted to DB so sessions survive container restarts
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<InsurancePlan> InsurancePlans => Set<InsurancePlan>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
        public DbSet<UserPolicy> UserPolicies => Set<UserPolicy>();
        public DbSet<AppSettings> AppSettings => Set<AppSettings>();
        public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
        public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<InsurancePlan>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.PlanName).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.MonthlyPremium).HasColumnType("numeric(10,2)");
                entity.Property(p => p.CoverageLimit).HasColumnType("numeric(14,2)");
                entity.Property(p => p.Deductible).HasColumnType("numeric(10,2)");
                entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(50);
                entity.Property(p => p.Tier).HasConversion<string>().HasMaxLength(30);
                entity.Property(p => p.Features).HasMaxLength(2000);

                entity.HasOne(p => p.Provider)
                      .WithMany(pv => pv.Plans)
                      .HasForeignKey(p => p.ProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Provider>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.ContactEmail).IsRequired().HasMaxLength(254);
                entity.Property(p => p.Website).HasMaxLength(300);
                entity.Property(p => p.Country).HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(1000);
            });

            builder.Entity<Client>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.FullName).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Email).IsRequired().HasMaxLength(254);
                entity.Property(c => c.Phone).HasMaxLength(30);
                entity.Property(c => c.Notes).HasMaxLength(1000);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<QuoteRequest>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.FullName).IsRequired().HasMaxLength(200);
                entity.Property(q => q.Email).IsRequired().HasMaxLength(254);
                entity.Property(q => q.PlanName).IsRequired().HasMaxLength(200);
                entity.Property(q => q.MonthlyPremium).HasColumnType("numeric(10,2)");
                entity.Property(q => q.InsuranceType).HasMaxLength(50);
                entity.Property(q => q.ProviderName).HasMaxLength(200);
                entity.Property(q => q.RequestedAt).HasDefaultValueSql("now()");
                entity.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(q => q.AdminNotes).HasMaxLength(1000);

                entity.HasOne(q => q.ConvertedPolicy)
                      .WithMany()
                      .HasForeignKey(q => q.ConvertedPolicyId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(q => q.Source).HasMaxLength(100);

                entity.HasOne(q => q.Plan)
                      .WithMany()
                      .HasForeignKey(q => q.PlanId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(q => q.Client)
                      .WithMany(c => c.Quotes)
                      .HasForeignKey(q => q.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserPolicy>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.PolicyName).IsRequired().HasMaxLength(200);
                entity.Property(p => p.ProviderName).IsRequired().HasMaxLength(200);
                entity.Property(p => p.InsuranceType).HasMaxLength(50);
                entity.Property(p => p.PolicyNumber).HasMaxLength(100);
                entity.Property(p => p.MonthlyPremium).HasColumnType("numeric(10,2)");
                entity.Property(p => p.CommissionRate).HasColumnType("numeric(5,2)");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

                entity.HasOne(p => p.Client)
                      .WithMany(c => c.Policies)
                      .HasForeignKey(p => p.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AppSettings>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.BusinessName).IsRequired().HasMaxLength(200);
                entity.Property(s => s.AgentContactEmail).HasMaxLength(254);
                entity.Property(s => s.CurrencySymbol).HasMaxLength(10);
                entity.Property(s => s.SmtpHost).HasMaxLength(200);
                entity.Property(s => s.SmtpSenderEmail).HasMaxLength(254);
                entity.Property(s => s.SmtpSenderName).HasMaxLength(200);
                entity.Property(s => s.SmtpSenderPassword).HasMaxLength(500);
                entity.Property(s => s.SmtpAgentEmail).HasMaxLength(254);
                entity.Property(s => s.AfricasTalkingUsername).HasMaxLength(200);
                entity.Property(s => s.AfricasTalkingApiKey).HasMaxLength(500);
                entity.Property(s => s.AfricasTalkingSenderId).HasMaxLength(50);
                entity.Property(s => s.LeadApiKey).HasMaxLength(500);
            });

            builder.Entity<WebhookEndpoint>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Url).IsRequired().HasMaxLength(500);
                entity.Property(w => w.Secret).HasMaxLength(500);
                entity.Property(w => w.Events).HasConversion<int>();
                entity.Property(w => w.LastStatus).HasMaxLength(200);
            });

            builder.Entity<WebhookDeliveryLog>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.EventType).IsRequired().HasMaxLength(50);
                entity.Property(l => l.ErrorMessage).HasMaxLength(1000);

                entity.HasOne(l => l.WebhookEndpoint)
                      .WithMany(w => w.DeliveryLogs)
                      .HasForeignKey(l => l.WebhookEndpointId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(30);
                entity.Property(t => t.Amount).HasColumnType("numeric(10,2)");
                entity.Property(t => t.PeriodMonth).IsRequired().HasMaxLength(7);
                entity.Property(t => t.Notes).HasMaxLength(500);

                entity.HasOne(t => t.Policy)
                      .WithMany()
                      .HasForeignKey(t => t.PolicyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Client)
                      .WithMany()
                      .HasForeignKey(t => t.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
