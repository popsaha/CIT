﻿namespace CIT.API.Models
{
    public class OrderRoutesMaster
    {
        public int? OrderRouteId { get; set; }
        public string RouteName { get; set; }
        public string? RouteDescription { get; set; }
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
