namespace SAMS.Models
{
    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int SessionID { get; set; }
        public int UserID { get; set; }
        public DateTime CheckInTime { get; set; }
        public virtual User User { get; set; }
        public virtual Session Session { get; set; }
        
    }
}
