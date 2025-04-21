namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class ParcelNo
    {
        public string ParcelQR { get; set; }
       
    }
    public class ParcelReceiptNos
    {
        //[Required(ErrorMessage = "ParcelQR is required.")]
        public List<ParcelInfo> ParcelQR { get; set; }
        public string PickupReceiptNumber { get; set; }
    }
    public class ParcelInfo
    {
        public string ParcelQR { get; set; }
    }
}
