﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SAMS.Data;

#nullable disable

namespace SAMS.Migrations
{
    [DbContext(typeof(AMSContext))]
    partial class AMSContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("SAMS.Models.Attendance", b =>
                {
                    b.Property<int>("AttendanceID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AttendanceID"));

                    b.Property<DateTime>("CheckInTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("SessionID")
                        .HasColumnType("int");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("AttendanceID");

                    b.HasIndex("SessionID");

                    b.HasIndex("UserID");

                    b.ToTable("Attendance", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.Course", b =>
                {
                    b.Property<int>("CourseID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CourseID"));

                    b.Property<string>("CourseName")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<int>("LecturerID")
                        .HasColumnType("int");

                    b.Property<int?>("Semester")
                        .HasColumnType("int");

                    b.HasKey("CourseID");

                    b.HasIndex("LecturerID");

                    b.ToTable("Course", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.CourseTime", b =>
                {
                    b.Property<int>("CourseTimeID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CourseTimeID"));

                    b.Property<int>("CourseID")
                        .HasColumnType("int");

                    b.Property<int>("Day")
                        .HasColumnType("int");

                    b.Property<TimeOnly>("EndTime")
                        .HasColumnType("time");

                    b.Property<TimeOnly>("StartTime")
                        .HasColumnType("time");

                    b.HasKey("CourseTimeID");

                    b.HasIndex("CourseID");

                    b.ToTable("CourseTime", null, t =>
                        {
                            t.HasCheckConstraint("CK_CourseTime_Day", "[Day] BETWEEN 0 AND 6");
                        });
                });

            modelBuilder.Entity("SAMS.Models.LectureHall", b =>
                {
                    b.Property<int>("LectureHallID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LectureHallID"));

                    b.Property<float>("MaxLatitude")
                        .HasColumnType("real");

                    b.Property<float>("MaxLongitude")
                        .HasColumnType("real");

                    b.Property<float>("MinLatitude")
                        .HasColumnType("real");

                    b.Property<float>("MinLongitude")
                        .HasColumnType("real");

                    b.Property<string>("Name")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.HasKey("LectureHallID");

                    b.ToTable("LectureHall", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.Lecturer", b =>
                {
                    b.Property<int>("LecturerID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.HasKey("LecturerID");

                    b.ToTable("Lecturer", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.Session", b =>
                {
                    b.Property<int>("SessionID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("SessionID"));

                    b.Property<int>("CourseID")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("ExpirationTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("LectureHallID")
                        .HasColumnType("int");

                    b.Property<string>("SessionCode")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.HasKey("SessionID");

                    b.HasIndex("CourseID");

                    b.HasIndex("LectureHallID");

                    b.ToTable("Session", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.Student", b =>
                {
                    b.Property<int>("StudentID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int?>("CurrentSemester")
                        .HasColumnType("int");

                    b.HasKey("StudentID");

                    b.ToTable("Student", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.User", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserID"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Name")
                        .IsUnicode(false)
                        .HasColumnType("varchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.Property<string>("UserType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)");

                    b.HasKey("UserID");

                    b.ToTable("User", (string)null);
                });

            modelBuilder.Entity("SAMS.Models.Attendance", b =>
                {
                    b.HasOne("SAMS.Models.Session", "Session")
                        .WithMany("Attendances")
                        .HasForeignKey("SessionID")
                        .IsRequired()
                        .HasConstraintName("FK_Course_Session");

                    b.HasOne("SAMS.Models.User", "User")
                        .WithMany("Attendances")
                        .HasForeignKey("UserID")
                        .IsRequired()
                        .HasConstraintName("FK_User_userID");

                    b.Navigation("Session");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SAMS.Models.Course", b =>
                {
                    b.HasOne("SAMS.Models.Lecturer", "Lecturer")
                        .WithMany("Courses")
                        .HasForeignKey("LecturerID")
                        .IsRequired()
                        .HasConstraintName("FK_Course_Lecturer");

                    b.Navigation("Lecturer");
                });

            modelBuilder.Entity("SAMS.Models.CourseTime", b =>
                {
                    b.HasOne("SAMS.Models.Course", "Course")
                        .WithMany("CourseTimes")
                        .HasForeignKey("CourseID")
                        .IsRequired()
                        .HasConstraintName("FK_Course_CourseID");

                    b.Navigation("Course");
                });

            modelBuilder.Entity("SAMS.Models.Lecturer", b =>
                {
                    b.HasOne("SAMS.Models.User", "User")
                        .WithOne("Lecturer")
                        .HasForeignKey("SAMS.Models.Lecturer", "LecturerID")
                        .IsRequired()
                        .HasConstraintName("FK_Lecturer_User");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SAMS.Models.Session", b =>
                {
                    b.HasOne("SAMS.Models.Course", "Course")
                        .WithMany("Sessions")
                        .HasForeignKey("CourseID")
                        .IsRequired()
                        .HasConstraintName("FK_Course_Course");

                    b.HasOne("SAMS.Models.LectureHall", "LectureHall")
                        .WithMany("Sessions")
                        .HasForeignKey("LectureHallID")
                        .IsRequired()
                        .HasConstraintName("FK_Course_LecturerHall");

                    b.Navigation("Course");

                    b.Navigation("LectureHall");
                });

            modelBuilder.Entity("SAMS.Models.Student", b =>
                {
                    b.HasOne("SAMS.Models.User", "User")
                        .WithOne("Student")
                        .HasForeignKey("SAMS.Models.Student", "StudentID")
                        .IsRequired()
                        .HasConstraintName("FK_Student_User");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SAMS.Models.Course", b =>
                {
                    b.Navigation("CourseTimes");

                    b.Navigation("Sessions");
                });

            modelBuilder.Entity("SAMS.Models.LectureHall", b =>
                {
                    b.Navigation("Sessions");
                });

            modelBuilder.Entity("SAMS.Models.Lecturer", b =>
                {
                    b.Navigation("Courses");
                });

            modelBuilder.Entity("SAMS.Models.Session", b =>
                {
                    b.Navigation("Attendances");
                });

            modelBuilder.Entity("SAMS.Models.User", b =>
                {
                    b.Navigation("Attendances");

                    b.Navigation("Lecturer");

                    b.Navigation("Student");
                });
#pragma warning restore 612, 618
        }
    }
}
