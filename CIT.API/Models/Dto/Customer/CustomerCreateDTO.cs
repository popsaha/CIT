using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Customer
{
    public class CustomerCreateDTO
    {
        [Required]
        [MaxLength(30)]
        public string? CustomerName { get; set; }
        [Required]
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }

    }
}
