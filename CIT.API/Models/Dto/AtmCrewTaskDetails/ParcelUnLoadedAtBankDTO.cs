using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class ParcelUnLoadedAtBankDTO
    {
        public string NextScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        public List<UnLoadedAtBrank> Parcels { get; set; }
        public string DeliveryReceiptNumber { get; set; }

    }
    public class UnLoadedAtBrank
    {
        public string ParcelQR { get; set; }
    }
}
