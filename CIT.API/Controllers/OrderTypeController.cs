using CIT.API.Models.Dto;
using CIT.API.Models;
using Microsoft.AspNetCore.Mvc;
using CIT.API.Repository.IRepository;

namespace CIT.API.Controllers
{
    [Route("api/OrderType")]
    [ApiController]
    public class OrderTypeController : ControllerBase
    {
        private readonly IOrderTypeRepository _orderTypeRepository;
        public OrderTypeController(IOrderTypeRepository IorderTypeRepository)
        {
            _orderTypeRepository = IorderTypeRepository;
        }
        [HttpGet("GetAllOrderTypeAPI")]
        public async Task<IActionResult> GetAllOrderType()
        {
            var orderTypeMasters = await _orderTypeRepository.GetAllOrderType();
            return Ok(orderTypeMasters);
        }

        [HttpPost("AddOrderTypeAPI")]
        public async Task<IActionResult> AddOrderType(OrderTypeDTO orderTypeDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    Res = await _orderTypeRepository.AddOrderType(orderTypeDTO);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpGet("GetOrderTypeAPI")]
        public async Task<IActionResult> GetOrderType([FromRoute(Name = "OrderTypeID")] int OrderTypeID)
        {
            OrderTypeMaster orderTypeMaster = new OrderTypeMaster();
            try
            {
                orderTypeMaster = await _orderTypeRepository.GetOrderType(OrderTypeID);
                return Ok(orderTypeMaster);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPut("UpdateOrderTypeAPI")]
        public async Task<IActionResult> UpdateOrderType(OrderTypeDTO OrderTypeDTO)
        {
            int Res = 0;
            try
            {
                Res = await _orderTypeRepository.UpdateOrderType(OrderTypeDTO);
                return Ok(Res);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpDelete("DeleteOrderTypeAPI")]
        public async Task<IActionResult> DeleteOrderType(int OrderTypeID, int deletedBy)
        {
            int Res = 0;
            try
            {
                Res = await _orderTypeRepository.DeleteOrderType(OrderTypeID, deletedBy);
                return Ok("The user is Deleted");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);

            }
        }
    }
}
