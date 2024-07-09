namespace ZimPortal.Models
{
    public class Outlet
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public int ShopDelayTakeAway { get; set; }
        public int ShopDelayDelivery { get; set; }
        public string Url { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageB64 { get; set; }
        public List<string> FoodTypes { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }
    }
}
