namespace CIT.API.Models.Dto.Police
{
    public class PoliceDTO
    {
        public int PoliceId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsActive { get; set; }
    }
}
