using CIT.API.Models;
using CIT.API.Models.Dto.Order;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderRepository
    {
        //IEnumerable<RouteMaster> GetRoutelist(int customerId);
        Task<int> CreateOrder(OrderDTO orderDTO);
        Task<OrderResponse> GetOrderDetails(int Response);
        //get Orderlist by date
        Task<IEnumerable<OrderListDTO>> GetOrdersWithTaskListAsync(DateTime date, string regionId);
    }
}
