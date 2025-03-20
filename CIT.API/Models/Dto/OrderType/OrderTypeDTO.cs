using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.OrderType
{
    public class OrderTypeDTO
    {
        public int OrderTypeID { get; set; }
        [Required]
        [MaxLength(30)]
        public string? TypeName { get; set; }
        [Required]
        public string? DataSource { get; set; }
    }
}
