namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskParcelDTO
    {
        public int UserId { get; set; }
        public int ScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        public List<Parcel> Parcels { get; set; }
    }
    public class Parcel
    {
        public string ParcelQR { get; set; }
    }
}
