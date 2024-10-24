using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SAMS.Data;
using SAMS.Endpoints;
using SAMS.Models;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Cors;
using System.Security.Claims;
using SAMS.Hubs;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

// Add rate limiting services
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add services to the container.
//builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddLogging();
// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);


builder.Configuration.AddEnvironmentVariables();
// Update your JWT configuration
var jwtIssuer = builder.Configuration["JWT:ValidIssuer"];
var jwtAudience = builder.Configuration["JWT:ValidAudience"];
var jwtSecret = builder.Configuration["JWT:Secret"];

if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT configuration is incomplete. Please check your environment variables.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("DefaultConnection string is not configured.");
}

builder.Services.AddDbContext<AMSContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
       .AddEntityFrameworkStores<AMSContext>()
       .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(20);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});


// Configure JSON options for Minimal API
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ??
             throw new InvalidOperationException("JWT:Secret is not configured"))),
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

builder.Services.AddAuthorization();
// Add SignalR to Program.cs
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Add this to your service configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("https://students-attendance-management-system.vercel.app",
            "https://students-attendance-management-system-dinelkathilinas-projects.vercel.app",
            "http://localhost:3000") // Replace with your React app's URL
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
        /*builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();*/
    });
});



var app = builder.Build();

// Initialize roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Student", "Lecturer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    // For production, remove or comment out these lines
    // app.UseSwagger();
    // app.UseSwaggerUI(...);
}

// Replace the redirect to Swagger with a simple message for production
app.MapGet("/", () => app.Environment.IsDevelopment()
    ? Results.Redirect("/swagger/index.html")
    : Results.Ok("API is running"));



// Configure rate limiting middleware
app.UseIpRateLimiting();

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapLecturerCourseManagementEndpoints();
app.MapAuthEndpoints();
app.MapUserProfileEndpoints();
app.UseDeveloperExceptionPage();
app.MapLecturerEndpoints();
app.MapSessionEndpoints();
app.MapAttendanceReportEndpoints();
app.MapStudentAttendanceReportEndpoints();
app.MapStudentScheduleEndpoints();

//var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
//app.Run($"http://0.0.0.0:{port}");

app.MapHub<AttendanceHub>("/attendanceHub");


var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://+:{port}");
app.Run();
//Small change

