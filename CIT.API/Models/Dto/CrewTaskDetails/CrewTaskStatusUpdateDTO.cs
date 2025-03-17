using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.CrewTaskDetails
{
    public class CrewTaskStatusUpdateDTO
    {
        //[Required(ErrorMessage = "UserId is required.")]
        //public Guid UUID { get; set; }
        [Required(ErrorMessage = "Next ScreenId is required.")]
        public string NextScreenId { get; set; }
       
        public DateTime Time { get; set; } = DateTime.UtcNow;
      
        public Location Location { get; set; }
        

    }

    public class Location
    {
        //[Required(ErrorMessage = "Lat is required.")]
        public string Lat { get; set; }
        //[Required(ErrorMessage = "Long is required.")]
        public string Long { get; set; }
    }
    
}
