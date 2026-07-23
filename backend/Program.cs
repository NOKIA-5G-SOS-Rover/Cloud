using backend.Data;
using backend.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
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
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var migrated = false;
    var retries = 0;

    while (!migrated && retries < 10)
    {
        try
        {
            dbContext.Database.Migrate();
            migrated = true;
        }
        catch
        {
            retries++;
            Thread.Sleep(3000);
        }
    }

    if (!migrated)
    {
        throw new Exception("Database migration failed after multiple retries.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseStaticFiles();
app.MapControllers();
app.MapHub<DashboardHub>("/hub/dashboard");

app.MapGet("/", () => "Nokia 5G SOS Rover Cloud API is running.");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Backend is running"
}));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
app.Run();