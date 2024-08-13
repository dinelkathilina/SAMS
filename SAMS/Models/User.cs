using System;
using System.Collections.Generic;

namespace SAMS.Models;

public partial class User
{
    public int UserID { get; set; }

    public string? Name { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public virtual Lecturer? Lecturer { get; set; }

    public virtual Student? Student { get; set; }
}
