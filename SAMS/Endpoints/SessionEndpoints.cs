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
    //Timezone
    private static readonly TimeZoneInfo sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
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
            try
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                logger.LogInformation($"Attempting to create session for user: {userId}");

                // Combine date and time strings, then convert to DateTimeOffset
                var lectureStart = DateTimeOffset.ParseExact($"{model.Date} {model.LectureStartTime}", "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.AssumeLocal);
                var lectureEnd = DateTimeOffset.ParseExact($"{model.Date} {model.LectureEndTime}", "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.AssumeLocal);

                var creationTime = DateTimeOffset.Now;
                var expirationTime = creationTime.AddMinutes(model.QRCodeExpirationMinutes);

                // Convert all times to UTC
                var utcCreationTime = creationTime.UtcDateTime;
                var utcExpirationTime = expirationTime.UtcDateTime;
                var utcLectureStartTime = lectureStart.UtcDateTime;
                var utcLectureEndTime = lectureEnd.UtcDateTime;

                // Check for overlapping sessions
                var overlappingSessions = await dbContext.Sessions
                    .Where(s => s.LectureHallID == model.LectureHallID &&
                                s.LectureStartTime < utcLectureEndTime &&
                                s.LectureEndTime > utcLectureStartTime)
                    .ToListAsync();

                if (overlappingSessions.Any())
                {
                    return Results.BadRequest("There is already a session scheduled in this lecture hall during the specified time.");
                }

                var newSession = new Session
                {
                    CourseID = model.CourseID,
                    LectureHallID = model.LectureHallID,
                    SessionCode = SessionCodeGenerator.GenerateSessionCode(model.CourseID, model.LectureHallID, utcCreationTime),
                    CreationTime = utcCreationTime,
                    ExpirationTime = utcExpirationTime,
                    LectureStartTime = utcLectureStartTime,
                    LectureEndTime = utcLectureEndTime
                };

                dbContext.Sessions.Add(newSession);
                await dbContext.SaveChangesAsync();

                logger.LogInformation($"Session created successfully. SessionID: {newSession.SessionID}, SessionCode: {newSession.SessionCode}");
                return Results.Ok(new
                {
                    SessionID = newSession.SessionID,
                    SessionCode = newSession.SessionCode,
                    CreationTime = newSession.CreationTime,
                    ExpirationTime = newSession.ExpirationTime,
                    LectureStartTime = newSession.LectureStartTime,
                    LectureEndTime = newSession.LectureEndTime
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating session");
                return Results.Problem($"An error occurred while creating the session: {ex.Message}", statusCode: 500);
            }
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
            var utcNow = DateTime.UtcNow;

            var activeSession = await dbContext.Sessions
                .Where(s => s.Course.Lecturer.UserID == userId && s.ExpirationTime > utcNow)
                .OrderByDescending(s => s.CreationTime)
                .Select(s => new {
                    s.SessionID,
                    s.SessionCode,
                    s.CourseID,
                    s.LectureHallID,
                    s.CreationTime,
                    s.ExpirationTime,
                    s.LectureEndTime,
                    RemainingTime = (int)(s.ExpirationTime - utcNow).TotalSeconds
                })
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                logger.LogInformation($"No active session found for user: {userId}");
                return Results.NotFound(new { message = "No active session found" });
            }

            var result = new
            {
                activeSession.SessionID,
                activeSession.SessionCode,
                activeSession.CourseID,
                activeSession.LectureHallID,
                CreationTime = TimeZoneInfo.ConvertTimeFromUtc(activeSession.CreationTime, sriLankaTimeZone),
                ExpirationTime = TimeZoneInfo.ConvertTimeFromUtc(activeSession.ExpirationTime, sriLankaTimeZone),
                LectureEndTime = TimeZoneInfo.ConvertTimeFromUtc(activeSession.LectureEndTime, sriLankaTimeZone),
                activeSession.RemainingTime
            };

            logger.LogInformation($"Active session found: {activeSession.SessionID}");
            return Results.Ok(result);
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
    public string Date { get; set; }
    public string LectureStartTime { get; set; }
    public string LectureEndTime { get; set; }
    public int QRCodeExpirationMinutes { get; set; }
}
public class CheckInModel
{
    public string SessionCode { get; set; }
}