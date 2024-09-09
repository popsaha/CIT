﻿namespace CIT.API.Models.Dto
{
    public class OrderTypeDTO
    {
        public int OrderTypeID { get; set; }
        public string? TypeName { get; set; }
        public string? DataSource { get; set; }
        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeleteBy { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
