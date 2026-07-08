using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDatabase : DbContext
{
    public AppDatabase(DbContextOptions<AppDatabase> options)
        : base(options)
    {
    }

    public DbSet<SOSAlert> SOSAlerts => Set<SOSAlert>();

    public DbSet<RoverSession> RoverSessions => Set<RoverSession>();
}