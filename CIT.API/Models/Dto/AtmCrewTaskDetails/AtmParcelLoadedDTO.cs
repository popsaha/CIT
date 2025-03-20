using CIT.API.Models.Dto.CrewTaskDetails;
using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class AtmParcelLoadedDTO
    {
       
        public string NextScreenId { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string PickupReceiptNumber { get; set; }
        [Required]
        public Location Location { get; set; }
        public string ParcelNumber { get; set; }
    }
    public class AtmParcelCountDTO
    {
        public int ParcelLoadedAtBank {  get; set; }
        public int ParcelLoadedAtAtm {get;set; }
    }
   
}
