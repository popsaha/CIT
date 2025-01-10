using CIT.API.Models;
using CIT.API.Models.Dto.OrderRoute;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderRouteRepository
    {

        Task<IEnumerable<OrderRouteDTO>> GetAllOrderRoutesAsync();

        Task<OrderRouteDTO> GetSingleOrderRoutesAsync(int id);
        Task<int> CreateOrderRoutesAsync(OrderRouteCreateDTO routeCreateDTO, int userId);
        //  updating order routes
        Task<APIResponse> UpdateOrderRouteAsync(OrderRouteUpdateDTO orderUpdateRouteDTO);

        Task<int> DeleteOrderAsync(int id, int deletedBy);
        Task<RouteUpdateDTO> RouteOrderUpdateAsync(RouteUpdateDTO routeUpdate);
    }
}
