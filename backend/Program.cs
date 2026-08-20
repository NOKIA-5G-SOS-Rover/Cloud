using backend.Data;
using backend.Hubs;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.Configure<CameraStreamOptions>(
    builder.Configuration.GetSection(CameraStreamOptions.SectionName)
);

builder.Services.AddSingleton<CameraRegistry>();

builder.Services.AddHttpClient(CameraPullWorker.HttpClientName, client =>
{
    // An MJPEG response never completes, so the only useful timeout is on the
    // initial connect (configured on the handler below).
    client.Timeout = Timeout.InfiniteTimeSpan;
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectTimeout = TimeSpan.FromSeconds(5)
});

builder.Services.AddHostedService<CameraPullWorker>();
builder.Services.AddHostedService<CameraStatusNotifier>();


var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing."
    );

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 36))
    );
});

builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<RoverControlService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddCors(options =>

{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://92.87.91.146:5000",
                "http://92.87.91.146"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "login",
        httpContext =>
        {
            var ip =
                httpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
                ?? "unknown";

            return RateLimitPartition
                .GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,

                        Window =
                            TimeSpan.FromMinutes(1),

                        QueueLimit = 0,

                        QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst
                    });
        });
});

var app = builder.Build();

// Aplicarea automată a migrărilor, cu maximum 10 încercări
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var migrated = false;
    var retries = 0;

    while (!migrated && retries < 10)
    {
        try
        {
            dbContext.Database.Migrate();
            migrated = true;

            Console.WriteLine(
                "Baza de date MySQL a fost migrată cu succes!"
            );
        }
        catch (Exception exception)
        {
            retries++;

            Console.WriteLine(
                $"Eroare la migrarea MySQL, încercarea {retries}: " +
                exception.Message
            );

            if (retries < 10)
            {
                Thread.Sleep(3000);
            }
        }
    }

    if (!migrated)
    {
        throw new Exception(
            "Database migration failed after multiple retries."
        );
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseMiddleware<SessionMiddleware>();

app.MapControllers();


app.MapControllers();

app.MapHub<DashboardHub>("/dashboardHub");

app.MapGet("/", () =>
    "Nokia 5G SOS Rover Cloud API is running."
);

app.MapGet(
    "/health",
    async (
        AppDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var databaseConnected =
                await dbContext.Database
                    .CanConnectAsync(
                        cancellationToken);

            var pendingMigrations =
                await dbContext.Database
                    .GetPendingMigrationsAsync(
                        cancellationToken);

            var response = new
            {
                status =
                    databaseConnected
                        ? "Healthy"
                        : "Unhealthy",

                backend = "Running",

                database = new
                {
                    connected =
                        databaseConnected,

                    pendingMigrations =
                        pendingMigrations.Count()
                },

                timestamp =
                    DateTime.UtcNow
            };

            if (!databaseConnected)
            {
                return Results.Json(
                    response,
                    statusCode: 503
                );
            }

            return Results.Ok(response);
        }
        catch
        {
            return Results.Json(
                new
                {
                    status = "Unhealthy",
                    backend = "Running",
                    database = new
                    {
                        connected = false
                    },
                    timestamp =
                        DateTime.UtcNow
                },
                statusCode: 503
            );
        }
    }
);

app.Run();
