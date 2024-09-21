using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.OrderType
{
    public class OrderTypeCreateDTO
    {
        [Required]
        [MaxLength(30)]
        public string? TypeName { get; set; }
        [Required]
        public string? DataSource { get; set; }
        public bool? IsActive { get; set; }
    }
}
