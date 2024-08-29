using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class Student
{
    public int StudentID { get; set; }
    public string UserID { get; set; } = null!;

    public int? CurrentSemester { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
