using CIT.API.Models.Dto;
using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IOrderTypeRepository
    {
        public Task<IEnumerable<OrderTypeMaster>> GetAllOrderType();
        Task<int> AddOrderType(OrderTypeDTO customerDTO);
        Task<OrderTypeMaster> GetOrderType(int customerId);
        Task<int> DeleteOrderType(int OrderTypeID, int deletedBy);
        Task<int> UpdateOrderType(OrderTypeDTO orderTypeDTO);
    }
}
