using CIT.API.Models;
using CIT.API.Models.Dto.OrderType;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderTypeRepository
    {
        public Task<IEnumerable<OrderTypeMaster>> GetAllOrderType();
        Task<int> AddOrderType(OrderTypeCreateDTO customerDTO);
        Task<OrderTypeMaster> GetOrderType(int customerId);
        Task<int> DeleteOrderType(int OrderTypeID, int deletedBy);
        Task<int> UpdateOrderType(OrderTypeMaster orderTypeDTO);
    }
}
