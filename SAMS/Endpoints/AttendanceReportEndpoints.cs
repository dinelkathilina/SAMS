using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAMS.Data;
using SAMS.Models;
using System.Security.Claims;

public static class AttendanceReportEndpoints
{
    public static void MapAttendanceReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/attendance-report", [Authorize(Roles = "Lecturer")] async (HttpContext httpContext, AMSContext dbContext, ILogger<Program> logger) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var lecturer = await dbContext.Lecturers
                    .Include(l => l.Courses)
                        .ThenInclude(c => c.Sessions)
                            .ThenInclude(s => s.Attendances)
                                .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(l => l.UserID == userId);

                if (lecturer == null)
                {
                    return Results.NotFound("Lecturer not found");
                }

                var attendanceReport = lecturer.Courses.Select(course => new
                {
                    CourseId = course.CourseID,
                    CourseName = course.CourseName,
                    TotalSessions = course.Sessions.Count,
                    Students = course.Sessions
                        .SelectMany(s => s.Attendances)
                        .GroupBy(a => a.User)
                        .Select(g => new
                        {
                            StudentId = g.Key.Id,
                            StudentName = g.Key.Name,
                            AttendedSessions = g.Count(),
                            AttendancePercentage = (double)g.Count() / course.Sessions.Count * 100
                        })
                        .OrderBy(s => s.StudentName)
                        .ToList()
                }).ToList();

                return Results.Ok(attendanceReport);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while generating attendance report");
                return Results.Problem("An error occurred while generating the attendance report. Please try again later.");
            }
        })
        .WithName("GetAttendanceReport")
        .WithOpenApi();
    }
}