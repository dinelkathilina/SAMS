﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAMS.Data;
using SAMS.Models;
using System.Security.Claims;

public static class LecturerCourseManagementEndpoints
{
    public static void MapLecturerCourseManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manage-courses/courses", [Authorize(Roles = "Lecturer")] async (HttpContext httpContext, AMSContext context) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var lecturer = await context.Lecturers
                .Include(l => l.Courses)
                    .ThenInclude(c => c.CourseTimes)
                .FirstOrDefaultAsync(l => l.UserID == userId);

            if (lecturer == null)
                return Results.NotFound("Lecturer not found");

            var courses = lecturer.Courses.Select(c => new
            {
                c.CourseID,
                c.CourseName,
                c.Semester,
                CourseTimes = c.CourseTimes.Select(ct => new
                {
                    ct.Day,
                    StartTime = ct.StartTime.ToString(@"HH:mm"),
                    EndTime = ct.EndTime.ToString(@"HH:mm")
                }).ToList()
            }).ToList();

            return Results.Ok(courses);
        });

        app.MapPost("/api/manage-courses/courses", [Authorize(Roles = "Lecturer")] async (HttpContext httpContext, AMSContext context, CourseCreationDto courseDto) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var lecturer = await context.Lecturers.FirstOrDefaultAsync(l => l.UserID == userId);
            if (lecturer == null)
                return Results.NotFound("Lecturer not found");

            var newCourse = new Course
            {
                CourseName = courseDto.CourseName,
                Semester = courseDto.Semester,
                LecturerID = lecturer.LecturerID
            };

            context.Courses.Add(newCourse);
            await context.SaveChangesAsync();

            foreach (var timeSlot in courseDto.CourseTimes)
            {
                var courseTime = new CourseTime
                {
                    CourseID = newCourse.CourseID,
                    Day = timeSlot.Day,
                    StartTime = TimeOnly.Parse(timeSlot.StartTime),
                    EndTime = TimeOnly.Parse(timeSlot.EndTime)
                };
                context.CourseTimes.Add(courseTime);
            }

            await context.SaveChangesAsync();

            return Results.Created($"/api/manage-courses/courses/{newCourse.CourseID}", newCourse);
        });

        app.MapPut("/api/manage-courses/courses/{id}", [Authorize(Roles = "Lecturer")] async (HttpContext httpContext, AMSContext context, int id, CourseUpdateDto courseDto) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var course = await context.Courses
                .Include(c => c.CourseTimes)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.Lecturer.UserID == userId);

            if (course == null)
                return Results.NotFound("Course not found or you don't have permission to edit it");

            course.CourseName = courseDto.CourseName;
            course.Semester = courseDto.Semester;

            // Remove existing course times
            context.CourseTimes.RemoveRange(course.CourseTimes);

            // Add new course times
            foreach (var timeSlot in courseDto.CourseTimes)
            {
                var courseTime = new CourseTime
                {
                    CourseID = course.CourseID,
                    Day = timeSlot.Day,
                    StartTime = TimeOnly.Parse(timeSlot.StartTime),
                    EndTime = TimeOnly.Parse(timeSlot.EndTime)
                };
                context.CourseTimes.Add(courseTime);
            }

            await context.SaveChangesAsync();

            return Results.NoContent();
        });

        app.MapDelete("/api/manage-courses/courses/{id}", [Authorize(Roles = "Lecturer")] async (HttpContext httpContext, AMSContext context, int id) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            var course = await context.Courses
                .Include(c => c.CourseTimes)
                .FirstOrDefaultAsync(c => c.CourseID == id && c.Lecturer.UserID == userId);

            if (course == null)
                return Results.NotFound("Course not found or you don't have permission to delete it");

            // Remove related CourseTimes
            context.CourseTimes.RemoveRange(course.CourseTimes);

            // Remove the course
            context.Courses.Remove(course);

            await context.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}

public class CourseCreationDto
{
    public string CourseName { get; set; } = null!;
    public int? Semester { get; set; }
    public List<CourseTimeDto> CourseTimes { get; set; } = new List<CourseTimeDto>();
}

public class CourseUpdateDto
{
    public string CourseName { get; set; } = null!;
    public int? Semester { get; set; }
    public List<CourseTimeDto> CourseTimes { get; set; } = new List<CourseTimeDto>();
}

public class CourseTimeDto
{
    public DayOfWeek Day { get; set; }
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
}