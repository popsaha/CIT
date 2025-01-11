namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskUnloadedParcelDTOs
    {
        public string NextScreenId { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string DeliveryReceiptNumber { get; set; }
        public Location Location { get; set; }

        public List<Parcel> Parcels { get; set; }

    }
    
}
