using Microsoft.EntityFrameworkCore;

namespace yatt.Tasks.Api.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<WorkItemGroup> WorkItemGroups { get; set; }
        public DbSet<WorkItem> WorkItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Organization> Organizations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkItemGroup>().HasQueryFilter(g => !g.IsDeleted);
            modelBuilder.Entity<WorkItem>().HasQueryFilter(t => !t.IsDeleted);

            modelBuilder.Entity<WorkItemGroup>()
                .HasMany(tg => tg.CoWorkers)
                .WithMany(u => u.WorkItemGroups);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Members)
                .WithMany(u => u.Organizations);
        }
    }
}