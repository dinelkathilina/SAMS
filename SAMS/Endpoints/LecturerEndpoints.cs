using Microsoft.AspNetCore.Identity;
using SAMS.Data;
using SAMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


public static class LecturerEndpoints
{
    public static void MapLecturerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lecturer/courses", [Authorize] async (HttpContext httpContext, AMSContext context, UserManager<ApplicationUser> userManager) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Results.NotFound("User not found");

            var lecturer = await context.Lecturers
                .Include(l => l.Courses)
                    .ThenInclude(c => c.CourseTimes)
                .FirstOrDefaultAsync(l => l.UserID == userId);

            if (lecturer == null)
                return Results.NotFound("Lecturer not found");

            var currentTime = DateTime.Now;
            var coursesWithTimes = lecturer.Courses.Select(c => new
            {
                CourseId = c.CourseID,
                CourseName = c.CourseName,
                UpcomingLectures = c.CourseTimes
                    .Where(ct => ct.Day >= currentTime.DayOfWeek &&
                        (ct.Day > currentTime.DayOfWeek || (ct.Day == currentTime.DayOfWeek && ct.StartTime.ToTimeSpan() > currentTime.TimeOfDay)))
                    .OrderBy(ct => ct.Day)
                    .ThenBy(ct => ct.StartTime)
                    .Take(5)
                    .Select(ct => new
                    {
                        Day = ct.Day,
                        StartTime = ct.StartTime,
                        EndTime = ct.EndTime
                    }),
                EarlierLectures = c.CourseTimes
                    .Where(ct => ct.Day <= currentTime.DayOfWeek &&
                        (ct.Day < currentTime.DayOfWeek || (ct.Day == currentTime.DayOfWeek && ct.EndTime.ToTimeSpan() < currentTime.TimeOfDay)))
                    .OrderByDescending(ct => ct.Day)
                    .ThenByDescending(ct => ct.StartTime)
                    .Take(5)
                    .Select(ct => new
                    {
                        Day = ct.Day,
                        StartTime = ct.StartTime,
                        EndTime = ct.EndTime
                    })
            }).ToList();

            return Results.Ok(coursesWithTimes);
        })
        .WithName("GetLecturerCourses")
        .WithOpenApi();
    }
}