using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAMS.Data;
using SAMS.Hubs;
using SAMS.Models;
using SAMS.Utilities; 
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

        app.MapPost("/api/session/create", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext, SessionCreationModel model, ILogger<Program> logger) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Attempting to create session for user: {userId}");

            var lecturer = await dbContext.Lecturers
                .FirstOrDefaultAsync(l => l.UserID == userId);
            if (lecturer == null)
            {
                logger.LogWarning($"Lecturer not found for user ID: {userId}");
                return Results.BadRequest("Lecturer not found for the current user.");
            }

            var course = await dbContext.Courses
                .FirstOrDefaultAsync(c => c.CourseID == model.CourseID && c.LecturerID == lecturer.LecturerID);
            if (course == null)
            {
                logger.LogWarning($"Invalid course selection. CourseID: {model.CourseID}, LecturerID: {lecturer.LecturerID}");
                return Results.BadRequest($"Invalid course selection. CourseID: {model.CourseID}, LecturerID: {lecturer.LecturerID}");
            }

            var lectureHall = await dbContext.LectureHalls.FindAsync(model.LectureHallID);
            if (lectureHall == null)
            {
                logger.LogWarning($"Invalid lecture hall selection. LectureHallID: {model.LectureHallID}");
                return Results.BadRequest("Invalid lecture hall selection.");
            }

            // Check for overlapping sessions
            var overlappingSessions = await dbContext.Sessions
                .AnyAsync(s => s.LectureHallID == model.LectureHallID &&
                               s.CreationTime < model.LectureEndTime &&
                               model.CreationTime < s.LectureEndTime);

            if (overlappingSessions)
            {
                logger.LogWarning($"Overlapping session detected for LectureHallID: {model.LectureHallID}, CreationTime: {model.CreationTime}, LectureEndTime: {model.LectureEndTime}");
                return Results.BadRequest("There is an overlapping session in the selected lecture hall.");
            }

            // Ensure times are in UTC
            var utcCreationTime = model.CreationTime.ToUniversalTime();
            var utcExpirationTime = model.ExpirationTime.ToUniversalTime();
            var utcLectureEndTime = model.LectureEndTime.ToUniversalTime();

            var sessionCode = SessionCodeGenerator.GenerateSessionCode(
                model.CourseID,
                model.LectureHallID,
                utcCreationTime
            );

            var newSession = new Session
            {
                CourseID = model.CourseID,
                LectureHallID = model.LectureHallID,
                SessionCode = sessionCode,
                CreationTime = utcCreationTime,
                ExpirationTime = utcExpirationTime,
                LectureEndTime = utcLectureEndTime
            };

            dbContext.Sessions.Add(newSession);
            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Session created successfully. SessionID: {newSession.SessionID}, SessionCode: {newSession.SessionCode}");
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


        app.MapGet("/api/session/active", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext, ILogger<Program> logger) =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation($"Fetching active session for user: {userId}");
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
                logger.LogInformation($"No active session found for user: {userId}");
                return Results.NotFound(new { message = "No active session found" });
            }

            logger.LogInformation($"Active session found: {activeSession.SessionID}");
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

            // Remove all related attendances
            var attendances = await dbContext.Attendances
                .Where(a => a.SessionID == sessionId)
                .ToListAsync();
            dbContext.Attendances.RemoveRange(attendances);

            // Remove the session
            dbContext.Sessions.Remove(session);

            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Session ended and all related data removed successfully" });
        });


        app.MapGet("/api/session/course-times/{courseId}", [Authorize(Roles = "Lecturer")] async (int courseId, AMSContext dbContext, ILogger<Program> logger) =>
        {
            logger.LogInformation($"Fetching course time for courseId: {courseId}");
            var currentDay = DateTime.UtcNow.DayOfWeek;
            var courseTime = await dbContext.CourseTimes
                .Where(ct => ct.CourseID == courseId && ct.Day == currentDay)
                .Select(ct => new { ct.StartTime, ct.EndTime })
                .FirstOrDefaultAsync();

            if (courseTime == null)
            {
                logger.LogWarning($"No course time found for courseId: {courseId} on day: {currentDay}");
                return Results.NotFound("No course time found for today");
            }

            logger.LogInformation($"Course time found for courseId: {courseId}");
            return Results.Ok(courseTime);
        });
    }
    
}

public class SessionCreationModel
{
    public int CourseID { get; set; }
    public int LectureHallID { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime ExpirationTime { get; set; }
    public DateTime LectureEndTime { get; set; }
}
public class CheckInModel
{
    public string SessionCode { get; set; }
}