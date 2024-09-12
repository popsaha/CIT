using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto
{
    public class BranchDTO
    {
        public int? BranchID { get; set; }
        [Required]
        [MaxLength(30)]
        public string? BranchName { get; set; }
        [Required]
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? DataSource { get; set; }
        public int CreatedBy { get; set; }
    }
}
