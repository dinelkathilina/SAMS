namespace SAMS.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int SessionID { get; set; }
        public string UserID { get; set; } = null!;
        public DateTime CheckInTime { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual Session Session { get; set; }
        
    }
}
