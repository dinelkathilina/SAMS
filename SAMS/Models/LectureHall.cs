namespace SAMS.Models
{
    public class LectureHall
    {
        public int LectureHallID { get; set; }
        public string Name { get; set; }
        public float MinLatitude { get; set; }
        public float MaxLatitude { get; set; }
        public float MinLongitude { get; set; }
        public float MaxLongitude { get; set; }
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    }
}
