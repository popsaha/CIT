namespace CIT.API.Models.Dto
{
    public class RegionDTO
    {
        public int RegionID { get; set; }
        public string? RegionName { get; set; }
        public string? DataSource { get; set; }
        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeleteBy { get; set; }
    }
}
