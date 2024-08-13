using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class Course
{
    public int CourseID { get; set; }

    public string CourseName { get; set; } = null!;

    public int? Semester { get; set; }

    public int LecturerID { get; set; }

    public virtual Lecturer Lecturer { get; set; } = null!;


    public virtual ICollection<CourseTime> CourseTimes { get; set; } = new List<CourseTime>();
}
