namespace CIT.API.Models
{
    public class PoliceMaster
    {
        public int PoliceId { get; set; }
        public string Name { get; set; }
        public int ThirdPartyProviderID { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string DataSource { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? IsDeleted { get; set; }
    }
}
