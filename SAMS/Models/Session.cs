namespace SAMS.Models
{
    public class Session
    {
        public int SessionID { get; set; }
        public int CourseID { get; set; }
        public int LectureHallID { get; set; }
        public string SessionCode { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime LectureEndTime { get; set; }
        public virtual Course Course { get; set; }
        public virtual LectureHall LectureHall  { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
