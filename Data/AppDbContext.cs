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
        public DbSet<BuildingRoom> BuildingsRooms { get; set; } = null!;
        public DbSet<Settings> Settings { get; set; } = null!;
        public DbSet<EventToGroupRef> EventToGroupRefs { get; set; } = null!;
        public DbSet<EventToPrepRef> EventToPrepRefs { get; set; } = null!;
        public DbSet<EventToBuildingRoomRef> EventToBuildingRoomRefs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BuildingRoom>()
                .HasOne(r => r.Building)
                .WithMany(b => b.Rooms)
                .HasForeignKey(r => r.MasterPtr);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Disc)
                .WithMany(d => d.Events)
                .HasForeignKey(e => e.DiscId);

            modelBuilder.Entity<Event>()
                .HasOne(e=>e.Chair)
                .WithMany(c=>c.Events)
                .HasForeignKey(e=>e.ChairId);

            modelBuilder.Entity<EventToGroupRef>()
                .HasOne(r => r.Event)
                .WithMany(e => e.GroupRefs)
                .HasForeignKey(r => r.MasterPtr);
            modelBuilder.Entity<EventToGroupRef>()
                .HasOne(r => r.Group)
                .WithMany(g => g.EventRefs)
                .HasForeignKey(r => r.GroupPtr);

            modelBuilder.Entity<EventToPrepRef>()
                .HasOne(r => r.Event)
                .WithMany(e => e.PrepRefs)
                .HasForeignKey(r => r.MasterPtr);
            modelBuilder.Entity<EventToPrepRef>()
                .HasOne(r => r.Prep)
                .WithMany(p => p.EventRefs)
                .HasForeignKey(r => r.PrepPtr);

            modelBuilder.Entity<EventToBuildingRoomRef>()
                .HasOne(r => r.Event)
                .WithMany(e => e.RoomRefs)
                .HasForeignKey(r => r.MasterPtr);
            modelBuilder.Entity<EventToBuildingRoomRef>()
                .HasOne(r => r.Room)
                .WithMany(room => room.EventRefs)
                .HasForeignKey(r => r.BuildingRoomPtr);

            modelBuilder.Entity<Event>().HasIndex(e => new { e.Week, e.Day, e.Less });
        }
    }
}