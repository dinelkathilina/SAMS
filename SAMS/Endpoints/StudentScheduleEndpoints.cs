using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SAMS.Data;
using SAMS.Models;

public static class StudentScheduleEndpoints
{
    public static void MapStudentScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/student/schedules", [Authorize(Roles = "Student")] async (
            HttpContext httpContext,
            AMSContext context,
            ILogger<Program> logger) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            try
            {
                var currentTime = DateTime.Now;
                var currentDay = currentTime.DayOfWeek;

                // Get all course times
                var courseTimes = await context.CourseTimes
                    .Include(ct => ct.Course)
                    .Where(ct => ct.Course.Semester == 1) // Add semester filter if needed
                    .OrderBy(ct => ct.Day)
                    .ThenBy(ct => ct.StartTime)
                    .ToListAsync();

                // Group and format course data
                var coursesWithTimes = courseTimes
                    .GroupBy(ct => new { ct.Course.CourseID, ct.Course.CourseName })
                    .Select(g => new
                    {
                        CourseId = g.Key.CourseID,
                        CourseName = g.Key.CourseName,
                        UpcomingLectures = g
                            .Where(ct => ct.Day >= currentDay &&
                                (ct.Day > currentDay ||
                                 (ct.Day == currentDay && ct.StartTime > TimeOnly.FromDateTime(currentTime))))
                            .OrderBy(ct => ct.Day)
                            .ThenBy(ct => ct.StartTime)
                            .Take(5)
                            .Select(ct => new
                            {
                                Day = ct.Day,
                                StartTime = ct.StartTime.ToString("HH:mm"),
                                EndTime = ct.EndTime.ToString("HH:mm")
                            })
                            .ToList(),
                        EarlierLectures = g
                            .Where(ct => ct.Day <= currentDay &&
                                (ct.Day < currentDay ||
                                 (ct.Day == currentDay && ct.EndTime < TimeOnly.FromDateTime(currentTime))))
                            .OrderByDescending(ct => ct.Day)
                            .ThenByDescending(ct => ct.StartTime)
                            .Take(5)
                            .Select(ct => new
                            {
                                Day = ct.Day,
                                StartTime = ct.StartTime.ToString("HH:mm"),
                                EndTime = ct.EndTime.ToString("HH:mm")
                            })
                            .ToList()
                    })
                    .ToList();

                return Results.Ok(coursesWithTimes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching student course schedule");
                return Results.Problem("An error occurred while fetching the course schedule.");
            }
        });

        app.MapGet("/api/student/schedules/today", [Authorize(Roles = "Student")] async (
            HttpContext httpContext,
            AMSContext context,
            ILogger<Program> logger) =>
        {
            try
            {
                var currentDay = DateTime.Now.DayOfWeek;
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                var todaySchedule = await context.CourseTimes
                    .Include(ct => ct.Course)
                    .Where(ct => ct.Day == currentDay)
                    .OrderBy(ct => ct.StartTime)
                    .Select(ct => new
                    {
                        CourseId = ct.CourseID,
                        CourseName = ct.Course.CourseName,
                        StartTime = ct.StartTime.ToString("HH:mm"),
                        EndTime = ct.EndTime.ToString("HH:mm"),
                        IsOngoing = ct.StartTime <= currentTime && currentTime <= ct.EndTime,
                        IsUpcoming = ct.StartTime > currentTime
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    CurrentDay = currentDay.ToString(),
                    CurrentTime = currentTime.ToString("HH:mm"),
                    Schedule = todaySchedule
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching today's schedule");
                return Results.Problem("An error occurred while fetching today's schedule.");
            }
        });
    }
}