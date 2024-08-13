using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class Lecturer
{
    public int LecturerID { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual User User { get; set; } = null!;
}
