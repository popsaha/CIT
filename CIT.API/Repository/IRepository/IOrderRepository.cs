using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderRepository
    {
        //IEnumerable<RouteMaster> GetRoutelist(int customerId);
        Task<int> CreateOrder(OrderDTO orderDTO);
        Task<OrderResponse> GetOrderDetails(int Response);

    }
}
