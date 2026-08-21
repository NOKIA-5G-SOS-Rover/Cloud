using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Middleware;

public class DatabaseMigrationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseMigrationMiddleware> _logger;

    private static readonly SemaphoreSlim MigrationLock = new(1, 1);
    private static bool _migrationChecked;

    public DatabaseMigrationMiddleware(
        RequestDelegate next,
        ILogger<DatabaseMigrationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IServiceScopeFactory scopeFactory)
    {
        if (!_migrationChecked)
        {
            await MigrationLock.WaitAsync();

            try
            {
                if (!_migrationChecked)
                {
                    using var scope = scopeFactory.CreateScope();

                    var dbContext =
                        scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var appliedMigrations =
                        await dbContext.Database
                            .GetAppliedMigrationsAsync();

                    var pendingMigrations =
                        await dbContext.Database
                            .GetPendingMigrationsAsync();

                    var lastAppliedMigration =
                        appliedMigrations.LastOrDefault();

                    _logger.LogInformation(
                        "Last applied database migration: {Migration}",
                        lastAppliedMigration ?? "None"
                    );

                    if (pendingMigrations.Any())
                    {
                        _logger.LogInformation(
                            "Found {Count} pending database migrations: {Migrations}",
                            pendingMigrations.Count(),
                            string.Join(", ", pendingMigrations)
                        );

                        await dbContext.Database.MigrateAsync();

                        _logger.LogInformation(
                            "Database migrations applied successfully."
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Database is already up to date."
                        );
                    }

                    _migrationChecked = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while applying database migrations."
                );

                throw;
            }
            finally
            {
                MigrationLock.Release();
            }
        }

        await _next(context);
    }
}