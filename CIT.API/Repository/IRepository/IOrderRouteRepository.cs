using CIT.API.Models;
using CIT.API.Models.Dto.OrderRoute;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderRouteRepository
    {

        Task<IEnumerable<OrderRouteDTO>> GetAllOrderRoutesAsync();


        //  updating order routes
        Task<APIResponse> UpdateOrderRouteAsync(OrderRouteUpdateDTO orderUpdateRouteDTO);
    }
}
