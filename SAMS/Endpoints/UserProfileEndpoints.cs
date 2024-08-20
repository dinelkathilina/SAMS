namespace SAMS.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SAMS.Models;
using System.Security.Claims;

public static class UserProfileEndpoints
{
    public static void MapUserProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/user/profile", [Authorize] async (HttpContext httpContext, UserManager<ApplicationUser> userManager, ILogger<Program> logger) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation($"Attempting to fetch profile for user ID: {userId}");

            if (userId == null)
            {
                logger.LogWarning("User ID not found in the token claims");
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning($"User not found for ID: {userId}");
                return Results.NotFound();
            }

            logger.LogInformation($"Successfully retrieved profile for user: {user.Email}");
            return Results.Ok(new
            {
                Name = user.Name,
                Email = user.Email
            });
        })
.RequireAuthorization()
.WithName("GetUserProfile")
.WithOpenApi();
    }
}