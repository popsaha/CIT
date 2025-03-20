using CIT.API.Models.Dto.CrewTaskDetails;
using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.BSSCrewTaskDetails
{
    public class BssParcelLoadDTO
    {
        //[Required(ErrorMessage = "ScreenId is required.")]
        public string NextScreenId { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string PickupReceiptNumber { get; set; }
        [Required]
        public Location Location { get; set; }

        public List<Parcel> Parcels { get; set; }
    }
    //public class Parcel
    //{
    //    [Required(ErrorMessage = "ParcelQR is required.")]
    //    public string ParcelQR { get; set; }
    //}
    //public class ParcelReceiptNo
    //{
    //    [Required(ErrorMessage = "ParcelQR is required.")]
    //    public string ParcelQR { get; set; }
    //    public string PickupReceiptNumber { get; set; }
    //}

    public class BssParcelCountDTO
    {
        public int ParcelLoaded { get; set; }
        public int ParcelUnloaded { get; set; }
    }
}
