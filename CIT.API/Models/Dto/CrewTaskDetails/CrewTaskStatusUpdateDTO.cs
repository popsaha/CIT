namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskStatusUpdateDTO
    {
     
        public int UserId { get; set; }
        public int ScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public Location Location { get; set; }
        

    }

    public class Location
    {
        public string Lat { get; set; }
        public string Long { get; set; }
    }
    
}
