namespace CIT.API.Models.Dto.Police
{
    public class PoliceUpdateDTO
    {
        public int PoliceId { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public bool? IsActive { get; set; }
        //public string IsActive { get; set; }
    }
}
