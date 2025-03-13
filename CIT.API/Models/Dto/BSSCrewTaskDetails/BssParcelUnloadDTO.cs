using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Models.Dto.BSSCrewTaskDetails
{
    public class BssParcelUnloadDTO
    {
        public string NextScreenId { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string DeliveryReceiptNumber { get; set; }
        public Location Location { get; set; }

        public List<Parcel> Parcels { get; set; }
    }
}
