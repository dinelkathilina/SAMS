using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAMS.Data;
using SAMS.Models;
using System.Security.Claims;

public static class StudentAttendanceReportEndpoints
{
    public static void MapStudentAttendanceReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/student/attendance-report", [Authorize(Roles = "Student")] async (HttpContext httpContext, AMSContext dbContext, ILogger<Program> logger) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            try
            {
                // Get the student's attendance records grouped by course
                var studentAttendance = await dbContext.Attendances
                    .Where(a => a.UserID == userId)
                    .Include(a => a.Session)
                        .ThenInclude(s => s.Course)
                    .GroupBy(a => new { a.Session.Course.CourseID, a.Session.Course.CourseName, a.Session.Course.Semester })
                    .Select(g => new
                    {
                        CourseId = g.Key.CourseID,
                        CourseName = g.Key.CourseName,
                        Semester = g.Key.Semester,
                        AttendedSessions = g.Count(),
                        // Get total sessions for this course
                        TotalSessions = dbContext.Sessions
                            .Count(s => s.CourseID == g.Key.CourseID),
                        // Get detailed attendance records
                        AttendanceDetails = g.Select(a => new
                        {
                            SessionDate = a.Session.CreationTime,
                            CheckInTime = a.CheckInTime
                        }).OrderByDescending(a => a.SessionDate).ToList()
                    })
                    .ToListAsync();

                var attendanceReport = studentAttendance.Select(course => new
                {
                    course.CourseId,
                    course.CourseName,
                    course.Semester,
                    course.AttendedSessions,
                    course.TotalSessions,
                    AttendancePercentage = course.TotalSessions > 0
                        ? (double)course.AttendedSessions / course.TotalSessions * 100
                        : 0,
                    course.AttendanceDetails
                }).OrderBy(c => c.Semester).ThenBy(c => c.CourseName).ToList();

                return Results.Ok(attendanceReport);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while generating student attendance report");
                return Results.Problem("An error occurred while generating the attendance report. Please try again later.");
            }
        })
        .WithName("GetStudentAttendanceReport")
        .WithOpenApi();
    }
}