﻿namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class ParcelNo
    {
        public string ParcelQR { get; set; }
       
    }
    public class ParcelReceiptNos
    {
        //[Required(ErrorMessage = "ParcelQR is required.")]
        public string ParcelQR { get; set; }
        public string PickupReceiptNumber { get; set; }
    }
}
