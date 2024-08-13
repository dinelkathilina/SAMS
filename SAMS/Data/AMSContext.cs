using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SAMS.Models;

namespace SAMS.Data;

public partial class AMSContext : DbContext
{
    public AMSContext(DbContextOptions<AMSContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Lecturer> Lecturers { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<CourseTime> CourseTimes { get; set; }
    public virtual DbSet<LectureHall> LectureHalls { get; set; }
    public virtual DbSet<Session> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");

            entity.Property(e => e.CourseName).IsUnicode(false);

            entity.HasOne(d => d.Lecturer).WithMany(p => p.Courses)
                .HasForeignKey(d => d.LecturerID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Course_Lecturer");

            
        });

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.HasKey(e => e.UserID);

            entity.ToTable("Lecturer");

            entity.Property(e => e.UserID).ValueGeneratedOnAdd();

            entity.HasOne(d => d.User).WithOne(p => p.Lecturer)
                .HasForeignKey<Lecturer>(d => d.UserID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lecturer_User");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.UserID);

            entity.ToTable("Student");

            entity.Property(e => e.UserID).ValueGeneratedOnAdd();

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Student_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Email).IsUnicode(false);
            entity.Property(e => e.Name).IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });
        
        modelBuilder.Entity<CourseTime>(entity =>
        {
            entity.ToTable("CourseTime");

            entity.HasOne(c => c.Course).WithMany(p => p.CourseTimes)
                .HasForeignKey(d => d.CourseID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Course_CourseID");

            entity.ToTable(tb => tb.HasCheckConstraint("CK_CourseTime_Day", "[Day] BETWEEN 0 AND 6"));


        });

        modelBuilder.Entity<LectureHall>(entity =>
        {
            entity.ToTable("LectureHall");

            entity.Property(e => e.Name).IsUnicode(false);

            


        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");

            entity.Property(e => e.SessionCode).IsUnicode(false);

            entity.HasOne(d => d.LectureHall).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.LectureHallID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Course_LecturerHall");
            
            entity.HasOne(d => d.Course).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.CourseID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Course_Course");


        });




        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
