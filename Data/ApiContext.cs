using CMS.Models;
using CMS.Models.Info;
using CMS.Models.Metrics;
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
        public DbSet<CarouselEntity> Carousels { get; set; } = null!;

        // Menu-related
        public DbSet<MenuEntity> Menus { get; set; } = null!;
        public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
        public DbSet<MenuItem> MenuItems { get; set; } = null!;

        // Metrics
        public DbSet<PageViewDaily> PageViewDailies { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Unique hostname across all tenants
            mb.Entity<TenantDomain>()
              .HasIndex(d => d.Hostname)
              .IsUnique();

            // -------- Global tenant filters --------
            mb.Entity<AboutEntity>()
              .HasQueryFilter(a => a.TenantId == _tenant.TenantId);

            mb.Entity<SocialsEntity>()
              .HasQueryFilter(s => s.TenantId == _tenant.TenantId);

            mb.Entity<AddressEntity>()
              .HasQueryFilter(a => a.TenantId == _tenant.TenantId);

            mb.Entity<CarouselEntity>()
              .HasQueryFilter(c => c.TenantId == _tenant.TenantId);

            mb.Entity<MenuEntity>()
              .HasQueryFilter(m => m.TenantId == _tenant.TenantId);

            mb.Entity<MenuCategory>()
              .HasQueryFilter(c => c.TenantId == _tenant.TenantId);

            mb.Entity<MenuItem>()
              .HasQueryFilter(i => i.TenantId == _tenant.TenantId);

            // -------- Relationships & cascade --------
            mb.Entity<MenuCategory>()
              .HasOne(c => c.Menu)
              .WithMany(m => m.Categories)
              .HasForeignKey(c => c.MenuEntityId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);

            mb.Entity<MenuItem>()
              .HasOne(i => i.Category)
              .WithMany(c => c.Items)
              .HasForeignKey(i => i.MenuCategoryId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);

            // -------- Helpful indexes --------
            mb.Entity<MenuEntity>()
              .HasIndex(m => m.TenantId)
              .IsUnique();

            mb.Entity<MenuCategory>()
              .HasIndex(c => new { c.TenantId, c.SortOrder });

            mb.Entity<MenuItem>()
              .HasIndex(i => new { i.TenantId, i.MenuCategoryId, i.SortOrder });

            // -------- Field sizes --------
            mb.Entity<MenuCategory>().Property(c => c.Name).HasMaxLength(120);

            mb.Entity<MenuItem>().Property(i => i.Name).HasMaxLength(160);
            mb.Entity<MenuItem>().Property(i => i.Price).HasMaxLength(32);
            mb.Entity<MenuItem>().Property(i => i.ImageUrl).HasMaxLength(512);

            // -------- Concurrency token --------
            mb.Entity<MenuEntity>().Property(m => m.RowVersion).IsRowVersion();

            // -------- Metrics --------
            mb.Entity<PageViewDaily>()
              .HasIndex(x => new { x.TenantId, x.Path, x.DayUtc })
              .IsUnique();
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var tid = _tenant.TenantId;
            foreach (var e in ChangeTracker.Entries())
            {
                if (e.State != EntityState.Added) continue;
                if (e.Entity is ITenantScoped ts &&
                    string.IsNullOrEmpty(ts.TenantId) &&
                    !string.IsNullOrEmpty(tid))
                {
                    ts.TenantId = tid!;
                }
            }
            return base.SaveChangesAsync(ct);
        }
    }
}
