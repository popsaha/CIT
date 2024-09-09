namespace CIT.API.Models.Dto
{
    public class BranchDTO
    {
        public int? BranchID { get; set; }
        public string? BranchName { get; set; }
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? DataSource { get; set; }
        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
