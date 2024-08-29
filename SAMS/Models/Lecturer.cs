using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class Lecturer
{
    public int LecturerID { get; set; }
    public string UserID { get; set; } = null!;


    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ApplicationUser User { get; set; } = null!;
}
