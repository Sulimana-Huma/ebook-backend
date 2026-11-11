using ebook_backend_api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://39.61.50.79",
                "http://39.61.50.79:8080",
                "https://39.61.50.79"
            )
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            Console.WriteLine("✅ Database connection successful!");
            Console.WriteLine($"📊 Database: {context.Database.GetDbConnection().Database}");
            Console.WriteLine($"🖥️ Server: {context.Database.GetDbConnection().DataSource}");
        }
        else
        {
            Console.WriteLine("❌ Database connection failed");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}


app.UseCors("AllowFrontend");
// In Program.cs or Startup.cs
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Content-Disposition")
);
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});
app.UseStaticFiles();

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 Application started successfully");
Console.WriteLine("🌐 HTTP: http://localhost:5158");

app.Run();
