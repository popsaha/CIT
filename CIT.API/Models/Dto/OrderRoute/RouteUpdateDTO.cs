namespace CIT.API.Models.Dto.OrderRoute
{
    public class RouteUpdateDTO
    {
        public int? OrderRouteId { get; set; }
        public string RouteName { get; set; }
        public string? RouteDescription { get; set; }
        public bool? IsActive { get; set; }
    }
}
