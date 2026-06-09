using System.Text;
using BackendAssignment.Api.Authorization;
using BackendAssignment.Api.Middlewares;
using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.ExternalServices.IpApi;
using BackendAssignment.Infrastructure.ExternalServices.Redis;
using BackendAssignment.Infrastructure.Helpers;
using BackendAssignment.Infrastructure.Persistence.Repositories;
using BackendAssignment.Infrastructure.Policies;
using Dapper;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure application to listen on http://localhost:5000 (for local development)
builder.WebHost.UseUrls("http://localhost:5000");

// Load and encode JWT secret key from config
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure Dapper to support snake_case DB fields
DefaultTypeMap.MatchNamesWithUnderscores = true;

// Add Controllers
builder.Services.AddControllers();

// External HTTP client with resilience policies (Retry, Circuit Breaker)
builder.Services
    .AddHttpClient<IIpLocationService, IpLocationService>()
    .AddPolicyHandler(PollyPolicyHelper.WrapResiliencePolicies());

// HttpContext accessor + custom geo-policy for authorization
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddCustomAuthorizationPolicies();

// Hangfire configuration using in-memory storage (for local/demo use)
builder.Services.AddTransient<IJobScheduler, JobScheduler>();
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// Helper for Dapper-based data access
builder.Services.AddSingleton<IDatabaseHelper, DatabaseHelper>();

// Register application repositories
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBulkInsertRepository, BulkInsertRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Register core services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBulkInsertService, BulkInsertService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Redis configuration (used for caching and job tracking)
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisSettings = builder.Configuration.GetSection("Redis");
    options.Configuration = redisSettings["ConnectionString"];
    options.InstanceName = redisSettings["InstanceName"];
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// JWT Authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow non-HTTPS for local dev
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Remove default clock skew
        };
    });

// Role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DeveloperOnly", policy => policy.RequireRole("Developer"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// Swagger/OpenAPI setup with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Books API",
        Version = "v1",
        Description = "This is the API for managing books, authors, and categories.\n\n" +
                                      "### Notes:\n\n" +
                                      "- **API Base URL**: The application runs on `http://localhost:5000`. You can access the Swagger UI at `http://localhost:5000/swagger`.\n" +
                                      "- **Authentication**: If an endpoint requires authentication, you will need a valid JWT token. For the test, use the provided credentials:\n" +
                                      "  - **Dev user**: `dev / dev`\n" +
                                      "  - **Regular user**: `user / user`\n" +
                                      "- **Rate-Limiting & Geo-Restriction**: The application includes a rate-limiting middleware and geo-restriction for requests coming from Greece, which is tested using Polly.\n" +
                                      "- **Resilience**: The application uses Polly for handling transient failures and retries in communication with external services (e.g., IP geolocation).\n" +
                                      "- **Background Jobs**: Some operations are handled asynchronously in the background using Hangfire.\n" +
                                      "- **Database Migrations**: Flyway handles database migrations to ensure smooth schema evolution during testing."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token without the Bearer prefix.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });
});

var app = builder.Build();

// Global exception handler to return consistent error responses
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandler?.Error;

        context.Response.ContentType = "application/json";

        switch (exception)
        {
            case BadRequestException badRequest:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ErrorResponse(badRequest.Message,
                    StatusCodes.Status400BadRequest));
                break;

            case NotFoundException notFound:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new ErrorResponse(notFound.Message,
                    StatusCodes.Status404NotFound));
                break;

            case ConflictException conflict:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new ErrorResponse(conflict.Message,
                    StatusCodes.Status409Conflict));
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ErrorResponse("An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError));
                break;
        }
    });
});

// Add routing support
app.UseRouting();

// Enable Swagger UI at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Books API v1");
    c.RoutePrefix = "swagger";
    c.InjectStylesheet("/swagger-ui/custom.css");
    c.InjectJavascript("/swagger-ui/custom.js");
});

// Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

// Middleware for rate-limiting and geo-restriction (Greece-only)
app.UseMiddleware<PollyRateLimitingMiddleware>();
app.UseMiddleware<GreeceAccessFailureMiddleware>();

// Background job dashboard (Hangfire)
app.UseHangfireDashboard();

// Map route to controllers
app.MapControllers();

// Default redirect to Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

// Open Swagger in browser when app starts (development only)
if (app.Environment.IsDevelopment())
{
    var swaggerUrl = "http://localhost:5000/swagger";
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = swaggerUrl,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not open Swagger UI automatically: {ex.Message}");
    }
}

// Run the app
app.Run();

// Expose Program class for integration tests
public partial class Program { }
