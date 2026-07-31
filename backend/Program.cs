using backend.Data;
using backend.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://92.87.91.146:5000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
            Console.WriteLine("Baza de date MySQL a fost migrata cu succes!");
        }
        catch (Exception exception)
        {
            retries++;

            Console.WriteLine(
                $"Database migration attempt {retries} failed: " +
                exception.Message
            );

            Thread.Sleep(3000);
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

app.MapControllers();

app.MapHub<DashboardHub>("/dashboardHub");

app.MapGet("/", () =>
    "Nokia 5G SOS Rover Cloud API is running."
);

app.MapGet("/health", () => Results.Ok(new
{
    status = "Backend is running"
}));

app.Run();