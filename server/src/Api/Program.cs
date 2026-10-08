using Application;
using Infrastructure;
using Api.Middleware;
using Api.Hubs;
using Api.Filters;
using Application.Common.Interfaces.Security;

using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// registers infrastructure services for dependency injection
builder.Services.AddInfrastructure(builder.Configuration);
// registers application services for dependency injection
builder.Services.AddApplicationServices();

// registers a global filter for incoming requests
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
// registers a SignalR hub for real-time communication or websockets
builder.Services.AddSignalR();

// registers a CORS policy for allowing origin clients
var allowedOrigins = new List<string> { "http://localhost:5173", "https://localhost:5173" };
var configuredClientUrl = builder.Configuration["CLIENT_URL"];
if (!string.IsNullOrWhiteSpace(configuredClientUrl))
{
    allowedOrigins.AddRange(configuredClientUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(allowedOrigins.Distinct().ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

// creates the application
var app = builder.Build();

// adds middleware for exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// migrate database on startup for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    dbContext.Database.Migrate();
    await DatabaseSeeder.SeedAsync(dbContext, passwordHasher);
}

app.UseCors("AllowClient");
// redirects http to https
app.UseHttpsRedirection();

// adds authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Aurum Messaging Server API");

// maps controllers and hubs
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();