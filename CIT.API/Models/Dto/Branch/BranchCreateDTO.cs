using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Branch
{
    public class BranchCreateDTO
    {
        [Required]
        [MaxLength(30)]
        public string? BranchName { get; set; }
        [Required]
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        //public string? DataSource { get; set; }
        //public int? CreatedBy { get; set; }
        public int CustomerID { get; set; }
        public int BranchCode { get; set; }
        public string? ReferenceNo1 { get; set; }
        public string? ReferenceNo2 { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
