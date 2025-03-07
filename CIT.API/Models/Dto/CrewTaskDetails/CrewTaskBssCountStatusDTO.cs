namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskBssCountStatusDTO
    {
        public string NextScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public int TotalAmount { get; set; }
        public Location Location { get; set; }
    }
}
