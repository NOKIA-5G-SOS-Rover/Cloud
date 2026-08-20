using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<UserSession> UserSessions { get; set; }

    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<RoverControlSession> RoverControlSessions { get; set; }

    public DbSet<Event> Events { get; set; }

    public DbSet<Device> Devices { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Telemetry> Telemetries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserSession>()
            .HasIndex(s => s.SessionId)
            .IsUnique();

        modelBuilder.Entity<UserPermission>()
            .HasIndex(p => new
            {
                p.UserId,
                p.Permission
            })
            .IsUnique();

        modelBuilder.Entity<UserPermission>()
            .HasOne(p => p.User)
            .WithMany(u => u.Permissions)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSession>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoverControlSession>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.CreatedAt);

        modelBuilder.Entity<Telemetry>()
            .HasIndex(t => new
            {
                t.RoverId,
                t.Timestamp
            });
    }
}