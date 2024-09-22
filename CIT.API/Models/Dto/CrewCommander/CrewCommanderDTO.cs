namespace CIT.API.Models.Dto.CrewCommander
{
    public class CrewCommanderDTO
    {
        public int CrewCommanderId { get; set; }
        public int EmployeeId { get; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
    }
}
