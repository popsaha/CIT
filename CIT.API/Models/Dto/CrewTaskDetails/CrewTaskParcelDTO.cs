﻿using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskParcelDTO
    {
        //[Required(ErrorMessage = "UserId is required.")]
        //public Guid UUID { get; set; }
        [Required(ErrorMessage = "ScreenId is required.")]
        public string NextScreenId { get; set; }
    
        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string PickupReceiptNumber { get; set; }
        [Required]
        public Location Location { get; set; }
     
        public List<Parcel> Parcels { get; set; }
    }
    public class Parcel
    {
        [Required(ErrorMessage = "ParcelQR is required.")]
        public string ParcelQR { get; set; }
    }

    public class ParcelReceiptNo
    {
        [Required(ErrorMessage = "ParcelQR is required.")]
        public string ParcelQR { get; set; }
        public string PickupReceiptNumber { get; set; }
    }

}
