namespace ZimPortal.Models
{
    public class TimeSlot
    {
        public TimeSlot() { }

        public TimeSpan Time { get; set; }
        public int Weekday { get; set; }
    }
}
