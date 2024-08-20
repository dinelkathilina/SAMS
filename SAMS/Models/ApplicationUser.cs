using Microsoft.AspNetCore.Identity;

namespace SAMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string UserType { get; set; } = null!;

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
