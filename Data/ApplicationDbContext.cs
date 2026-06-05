using Insurance_Hub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<InsurancePlan> InsurancePlans => Set<InsurancePlan>();
        public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();

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
            });
        }
    }
}
