using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAMS.Data;
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



        // You can add more session-related endpoints here in the future
        // For example:
        // app.MapPost("/api/session/create", [Authorize(Roles = "Lecturer")] async (HttpContext context, AMSContext dbContext, SessionCreationModel model) => { ... });
        // app.MapGet("/api/session/{id}", [Authorize] async (int id, AMSContext dbContext) => { ... });
        // etc.
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