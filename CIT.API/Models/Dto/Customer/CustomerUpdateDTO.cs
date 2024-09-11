using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Customer
{
    public class CustomerUpdateDTO
    {
        [Required]
        public int CustomerId { get; set; }
        [Required]
        [MaxLength(30)]
        public string? CustomerName { get; set; }
        [Required]
        public string? Address { get; set; }
        [Required]
        public string? ContactNumber { get; set; }
        [Required]
        public string? Email { get; set; }

    }
}
