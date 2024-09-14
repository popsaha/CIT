using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Region
{
    public class RegionUpdateDTO
    {
        [Required]
        public int RegionID { get; set; }
        [Required]
        [MaxLength(30)]
        public string? RegionName { get; set; }
        [Required]
        public string? DataSource { get; set; }
    }
}
