using Backend.DTOs;
using Backend.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Determine the Connection String & Client Base URL Based on Environment
string env = builder.Environment.IsDevelopment() ? "LocalDb" : "ProdDb";

// Load Configurations
builder.Services
    .AddDatabaseConfig(config, env)
    .AddCorsPolicy()
    .ConfigureRouting();

// Identity & Authentication
builder.Services
    .AddIdentityConfig()
    .AddJwtAuthentication(config)
    .AddAuthorizationPolicies();

// Register Services
builder.Services.AddApplicationServices();
builder.Services.Configure<FootballDataConfig>(config.GetSection("FootballData"));

// Swagger Configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Backend API", Version = "v1" });
});

// Build App
var app = builder.Build();

// Apply migrations & seed SuperAdmin via extension method
await app.ApplyMigrationsAndSeedSuperAdmin();

// Enable CORS
app.UseCors("AllowFrontend");

// Custom JWT Middleware Before Authentication
app.UseMiddleware<JwtMiddleware>();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Add static files from wwwroot
//app.UseDefaultFiles();
//app.UseStaticFiles();

//// Fallback to index.html for Angular Routing
//app.Use(async (context, next) =>
//{
//    await next();

//    if (context.Response.StatusCode == 404 &&
//        !Path.HasExtension(context.Request.Path.Value) &&
//        !context.Request.Path.Value.StartsWith("/api"))
//    {
//        context.Request.Path = "/index.html";
//        await next();
//    }
//});

app.MapControllers();
app.Run();
