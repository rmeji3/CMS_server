using CMS.Models;
using CMS.Models.Info;
using CMS.Services;
using Microsoft.EntityFrameworkCore;

namespace CMS.Data
{
    public class ApiContext : DbContext
    {
        private readonly ITenantProvider _tenant;

        public ApiContext(DbContextOptions<ApiContext> options, ITenantProvider tenant)
            : base(options) => _tenant = tenant;

        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<TenantDomain> TenantDomains { get; set; } = null!;
        public DbSet<AboutEntity> About { get; set; } = null!;
        public DbSet<SocialsEntity> Socials { get; set; } = null!;
        public DbSet<AddressEntity> Address { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Unique hostname across all tenants
            mb.Entity<TenantDomain>()
              .HasIndex(d => d.Hostname)
              .IsUnique();

            // Example global filter for About
            mb.Entity<AboutEntity>()
              .HasQueryFilter(a => a.TenantId == _tenant.TenantId);
            // Example global filter for Socials
            mb.Entity<SocialsEntity>()
                .HasQueryFilter(s => s.TenantId == _tenant.TenantId);
            // Example global filter for Address
            mb.Entity<AddressEntity>()
                .HasQueryFilter(a => a.TenantId == _tenant.TenantId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var tid = _tenant.TenantId;
            foreach (var e in ChangeTracker.Entries())
            {
                if (e.State != EntityState.Added) continue;
                if (e.Entity is ITenantScoped ts && string.IsNullOrEmpty(ts.TenantId) && !string.IsNullOrEmpty(tid))
                    ts.TenantId = tid!;
            }
            return base.SaveChangesAsync(ct);
        }
    }
}
