using Microsoft.EntityFrameworkCore;
using Xml.Api.Models;

namespace Xml.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Prep> Preps { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<Disc> Discs { get; set; } = null!;
        public DbSet<Chair> Chairs { get; set; } = null!;
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<BuildingsRoom> BuildingsRooms { get; set; } = null!;
        public DbSet<Settings> Settings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BuildingsRoom>()
                .HasOne(r => r.Building)
                .WithMany(b => b.Rooms)
                .HasForeignKey(r => r.BuildingName)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>().HasIndex(e => e.GroupId);
            modelBuilder.Entity<Event>().HasIndex(e => e.PrepId);

            modelBuilder.Entity<Event>().HasIndex(e => new { e.Week, e.Day, e.Less });
        }
    }
}
