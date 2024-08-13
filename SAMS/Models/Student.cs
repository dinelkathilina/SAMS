using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class Student
{
    public int UserID { get; set; }

    public int? CurrentSemester { get; set; }

    public virtual User User { get; set; } = null!;
}
