using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.OrderRoute
{
    public class OrderRouteUpdateDTO
    {
        [Required]
        public string RouteName { get; set; }
        [Required]
        public List<int> OrderIds { get; set; }

    }
}
