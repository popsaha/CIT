namespace CIT.API.Models
{
    public class BranchMaster
    {
        public int? BranchID { get; set; }
        public string? BranchName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string? DataSource { get; set; }
        public int? CreatedBy { get; set; }
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
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? IsDeleted { get; set; }
        public int? IsActive { get; set; }

    }
}
