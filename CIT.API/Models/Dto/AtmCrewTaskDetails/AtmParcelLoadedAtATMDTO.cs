using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class AtmParcelLoadedAtATMDTO
    {
        public string NextScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        public string ParcelNumber { get; set; }
    }
}
