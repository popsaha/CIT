﻿namespace CIT.API.Models
{
    public class CrewCommander
    {
        public int CrewCommanderId { get; set; }
        public int EmployeeId { get;}
        public  string Name { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string? DataSource { get; set; }
        public int? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? IsDeleted { get; set; }
    }
}
