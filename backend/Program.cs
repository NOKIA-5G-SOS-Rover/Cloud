var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("var1", () => "hello world");

app.MapGet("var2", () => Results.Ok(new
{
    status = "Backend is running",
}));

app.Run();