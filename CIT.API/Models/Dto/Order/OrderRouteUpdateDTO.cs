using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.Order
{
    public class OrderRouteUpdateDTO
    {
        [Required]
        public string RouteName { get; set; }
        [Required]
        public List<int> OrderIds { get; set; }

    }
}
