using Microsoft.EntityFrameworkCore;
using PartnerService.Core.Domain;

namespace PartnerService.DataAccess.Repositories
{
    public class DataContext
        : DbContext
    {

        public DbSet<Partner> Partners { get; set; } = null!;

        public DbSet<Activity> Activities { get; set; } = null!;

        public DbSet<Invite> Invites { get; set; } = null!;


        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Partner>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
                entity.Property(x => x.Specialization).HasMaxLength(250);
            });

            modelBuilder.Entity<Activity>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
            });

            modelBuilder.Entity<Invite>(entity =>
            {
                entity.Property(x => x.Code).HasMaxLength(100);
                entity.Property(x => x.ServiceInfo).HasMaxLength(500);
                entity.Property(x => x.Description).HasMaxLength(1000);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
