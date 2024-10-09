using CIT.API.Models;
using CIT.API.Models.Dto.Order;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        public readonly IOrderRepository _orderRepository;
        protected APIResponse _response;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _response = new();
            _logger = logger;
        }

        /// <summary>
        /// Creating a new order.
        /// </summary>
        /// <param name="orderDTO"></param>
        /// <returns></returns>
        [HttpPost("CreateOrder")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO orderDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    Res = await _orderRepository.CreateOrder(orderDTO);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }


        [HttpPost("GetOrderDetails")]
        public async Task<IActionResult> GetOrderDetails(int ResourceId)
        {
            OrderResponse orderResponse = new OrderResponse();
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    orderResponse = await _orderRepository.GetOrderDetails(ResourceId);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }




        /// <summary>
        /// Get order list by a specific date in "yyyy-MM-dd" format to show in the web interface.
        /// </summary>
        /// <param name="selectedDate">The date in "yyyy-MM-dd" format.</param>
        /// <returns>A list of orders for the specified date.</returns>
        [HttpGet("getOrdersList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrders([FromQuery] OrderDateDTO selectedDate)
        {
            try
            {
                if (!DateTime.TryParseExact(selectedDate.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime validDate))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid date format.");
                    return BadRequest(_response);
                }

                var orders = await _orderRepository.GetOrdersWithTaskListAsync(validDate);

                if (orders == null || !orders.Any())
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No orders found for the selected date.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    _response.Result = orders;
                    _response.StatusCode = HttpStatusCode.OK;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order ids");

                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return StatusCode((int)_response.StatusCode, _response);
        }

    }
}