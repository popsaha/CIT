namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskFailedStatusDTO
    {
        public int UserId { get; set; }
        public int ScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        public string FailureReason { get; set; }


    }
}