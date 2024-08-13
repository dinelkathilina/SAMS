namespace SAMS.Models
{
    public class CourseTime
    {
        public int CourseTimeID { get; set; }
        public int CourseID { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public virtual Course Course { get; set; } = null!;


    }
}
