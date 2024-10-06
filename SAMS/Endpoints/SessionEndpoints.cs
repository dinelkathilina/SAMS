using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SAMS.Data;
using SAMS.Hubs;
using SAMS.Models;
using System.Security.Claims;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/session/courses", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var courses = await dbContext.Courses
                .Where(c => c.Lecturer.UserID == userId)
                .Select(c => new { c.CourseID, c.CourseName })
                .ToListAsync();
            return Results.Ok(courses);
        });

        app.MapGet("/api/session/lecture-halls", [Authorize] async (AMSContext dbContext) =>
        {
            var lectureHalls = await dbContext.LectureHalls
                .Select(lh => new { lh.LectureHallID, lh.Name })
                .ToListAsync();
            return Results.Ok(lectureHalls);
        });

        app.MapPost("/api/session/create", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext, SessionCreationModel model) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lecturer = await dbContext.Lecturers
                .FirstOrDefaultAsync(l => l.UserID == userId);

            if (lecturer == null)
            {
                return Results.BadRequest("Lecturer not found for the current user.");
            }

            var course = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.CourseID == model.CourseID && c.LecturerID == lecturer.LecturerID);
            if (course == null)
            {
                return Results.BadRequest($"Invalid course selection. CourseID: {model.CourseID}, LecturerID: {lecturer.LecturerID}");
            }

            var lectureHall = await dbContext.LectureHalls.FindAsync(model.LectureHallID);
            if (lectureHall == null)
            {
                return Results.BadRequest("Invalid lecture hall selection.");
            }

            // Check for overlapping sessions
            var overlappingSessions = await dbContext.Sessions
                .AnyAsync(s => s.LectureHallID == model.LectureHallID &&
                               s.CreationTime < model.ExpirationTime &&
                               model.CreationTime < s.ExpirationTime);

            if (overlappingSessions)
            {
                return Results.BadRequest("There is an overlapping session in the selected lecture hall.");
            }

            var newSession = new Session
            {
                CourseID = model.CourseID,
                LectureHallID = model.LectureHallID,
                SessionCode = model.SessionCode,
                CreationTime = model.CreationTime,
                ExpirationTime = model.ExpirationTime
            };

            dbContext.Sessions.Add(newSession);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { SessionID = newSession.SessionID, SessionCode = newSession.SessionCode });
        });

        app.MapPost("/api/attendance/check-in", [Authorize(Roles = "Student")] async (HttpContext context, AMSContext dbContext,IHubContext<AttendanceHub> hubContext , ILogger<Program> logger, CheckInModel model) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Attempting check-in for user ID: {userId}");

            var student = await dbContext.Students
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (student == null)
            {
                logger.LogWarning($"Student not found for user ID: {userId}");
                return Results.BadRequest("Student not found for the current user.");
            }

            var session = await dbContext.Sessions
                .Include(s=>s.Course)
                .FirstOrDefaultAsync(s => s.SessionCode == model.SessionCode && s.ExpirationTime > DateTime.UtcNow);

            if (session == null)
            {
                return Results.BadRequest("Invalid or expired session code.");
            }

            // Check if the student has already checked in for this session
            var existingAttendance = await dbContext.Attendances
                .FirstOrDefaultAsync(a => a.SessionID == session.SessionID && a.UserID == userId);

            if (existingAttendance != null)
            {
                return Results.BadRequest("You have already checked in for this session.");
            }

            var user = await dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return Results.BadRequest("User not found.");
            }

            var newAttendance = new Attendance
            {
                SessionID = session.SessionID,
                UserID = userId,
                CheckInTime = DateTime.UtcNow,
                User = user,
                Session = session
            };

            dbContext.Attendances.Add(newAttendance);
            await dbContext.SaveChangesAsync();

            var attendanceInfo = new
            {
                StudentName = user.Name,
                CheckInTime = newAttendance.CheckInTime,
                CourseName  = session.Course.CourseName,
                StartTime   = session.CreationTime,
                EndTime     = session.ExpirationTime
            };
            await hubContext.Clients.Group(model.SessionCode).SendAsync("NewCheckIn", attendanceInfo);

            return Results.Ok(new 
            { 
                message = "Successfully checked in to the session.",
                sessionDetails = attendanceInfo
            });
        });

        // You can add more session-related endpoints here in the future
        // For example:
        // app.MapPost("/api/session/create", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext, SessionCreationModel model) => { ... });
        // app.MapGet("/api/session/{id}", [Authorize] async (int id, AMSContext dbContext) => { ... });
        // etc.
        // In SessionEndpoints.cs
        app.MapGet("/api/session/checked-in-students/{sessionCode}", [Authorize] async (string sessionCode, AMSContext dbContext) =>
        {
            var session = await dbContext.Sessions
                .Include(s => s.Attendances)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.SessionCode == sessionCode);

            if (session == null)
            {
                return Results.NotFound("Session not found");
            }

            var checkedInStudents = session.Attendances
                .Select(a => new
                {
                    StudentName = a.User.Name,
                    CheckInTime = a.CheckInTime
                })
                .ToList();

            return Results.Ok(checkedInStudents);
        });


        app.MapGet("/api/session/active", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;

            var activeSession = await dbContext.Sessions
                .Where(s => s.Course.Lecturer.UserID == userId && s.ExpirationTime > now)
                .OrderByDescending(s => s.CreationTime)
                .Select(s => new {
                    s.SessionID,
                    s.SessionCode,
                    s.CourseID,
                    s.LectureHallID,
                    s.CreationTime,
                    s.ExpirationTime,
                    RemainingTime = (int)(s.ExpirationTime - now).TotalSeconds
                })
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                return Results.NotFound(new { message = "No active session found" });
            }

            return Results.Ok(activeSession);
        });

        app.MapPost("/api/session/end/{sessionId}", [Authorize(Roles = "Lecturer")] async (int sessionId, HttpContext context, AMSContext dbContext) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var session = await dbContext.Sessions
                .Include(s => s.Course)
                .ThenInclude(c => c.Lecturer)
                .FirstOrDefaultAsync(s => s.SessionID == sessionId && s.Course.Lecturer.UserID == userId);

            if (session == null)
            {
                return Results.NotFound("Session not found or you don't have permission to end it.");
            }

            // We're not actually ending the session in the database
            // Just return a success message
            return Results.Ok(new { message = "Session ended successfully" });
        });
    }
    
}

public class SessionCreationModel
{
    public int CourseID { get; set; }
    public int LectureHallID { get; set; }
    public string SessionCode { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime ExpirationTime { get; set; }
}
public class CheckInModel
{
    public string SessionCode { get; set; }
}