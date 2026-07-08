using backend.Data;
using backend.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDatabase>(options =>
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
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDatabase>();
    dbContext.Database.Migrate(); //la pornire API-ul să aplice automat migrations și să creeze tabelele în MySQL dacă acestea nu există
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.MapControllers();
app.MapHub<DashboardHub>("/hub/dashboard");

app.MapGet("/", () => "Nokia 5G SOS Rover Cloud API is running.");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Backend is running"
}));

app.Run();