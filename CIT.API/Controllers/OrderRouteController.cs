using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.OrderRoute;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace CIT.API.Controllers
{
    [Route("api/OrderRoute")]
    [ApiController]
    public class OrderRouteController : ControllerBase
    {
        public readonly IOrderRouteRepository _orderRoutesRepository;
        protected APIResponse _response;
        private readonly ILogger<OrderRouteController> _logger;
        private readonly IMapper _mapper;

        public OrderRouteController(IOrderRouteRepository orderRouteRepo, ILogger<OrderRouteController> logger, IMapper mapper)
        {
            _orderRoutesRepository = orderRouteRepo;
            _response = new();
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet("GetAllOrderRoutes")]
        public async Task<IActionResult> GetAllOrderRoutes()
        {
            try
            {
                // Fetching all order routes
                var orderRoutes = await _orderRoutesRepository.GetAllOrderRoutesAsync();

                if (orderRoutes == null || !orderRoutes.Any())
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No order routes found.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    _response.Result = orderRoutes;
                    _response.StatusCode = HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return StatusCode((int)_response.StatusCode, _response);
        }


        [HttpGet("{id:int}", Name = "GetOrderRoute")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetOrderRoute(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var customer = await _orderRoutesRepository.GetSingleOrderRoutesAsync(id);

                if (customer == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                //_response.Result = customer;
                _response.Result = _mapper.Map<OrderRoutesMaster>(customer);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }


        [HttpPost]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateOrderRoute([FromBody] OrderRouteCreateDTO createDTO)
        {
            int Res = 0;
            try
            {

                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid OrderRoute data.");
                    return BadRequest(_response);
                }

                // Get the userId from the claims (JWT token)
                var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.Name));

                var customer = _mapper.Map<OrderRoutesMaster>(createDTO);

                Res = await _orderRoutesRepository.CreateOrderRoutesAsync(createDTO, userId);

                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("OrderRoute already exists.");
                    return BadRequest(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.Created;
                    _response.IsSuccess = true;
                    _response.Result = customer;
                    return Ok(_response);
                }
                // Return the created customer with the location of the new resource              
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }


        [HttpPut("{id:int}", Name = "UpdateOrderRoute")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateRoute(int id, [FromBody] RouteUpdateDTO updateDTO)
        {

            try
            {
                if (updateDTO == null || id != updateDTO.OrderRouteId)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Route Id.");

                    return BadRequest(_response);
                }

                RouteUpdateDTO routeUpdate = _mapper.Map<RouteUpdateDTO>(updateDTO);
                var updatedCustomer = await _orderRoutesRepository.RouteOrderUpdateAsync(routeUpdate);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                //return Problem(ex.Message, ex.StackTrace);
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }


        [HttpDelete("{routeId:int}", Name = "DeleteRoute")]
        //[Authorize(Roles = "CUSTOM")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteCustomer(int routeId, int userId)
        {
            try
            {
                if (routeId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid customer Id.");

                    return BadRequest(_response);
                }

                var route = await _orderRoutesRepository.GetSingleOrderRoutesAsync(routeId);

                if (route == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Route not found.");

                    return NotFound(_response);
                }

                var deletedRoute = await _orderRoutesRepository.DeleteOrderAsync(routeId, userId);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };

            }

            return _response;
        }


        /// <summary>
        /// If user wants to update/add the route of any order.
        /// </summary>
        /// <param name="orderUpdateRouteDTO"></param>
        /// <returns></returns>
        [HttpPost("updateRoute")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateOrderRoute([FromBody] OrderRouteUpdateDTO orderUpdateRouteDTO)
        {
            if (orderUpdateRouteDTO == null || !orderUpdateRouteDTO.OrderIds.Any())
            {
                _logger.LogWarning("Invalid input data: OrderRouteUpdateDTO is null or contains no OrderIds");
                return BadRequest(new APIResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { "Invalid input data" }
                });
            }

            // Trim the RouteName before calling the method
            orderUpdateRouteDTO.RouteName = orderUpdateRouteDTO.RouteName?.Trim();

            var response = await _orderRoutesRepository.UpdateOrderRouteAsync(orderUpdateRouteDTO);

            if (!response.IsSuccess)
            {
                _logger.LogError("Failed to update order route. StatusCode: {StatusCode}, Errors: {Errors}",
                        response.StatusCode, string.Join(", ", response.ErrorMessages));

                return StatusCode((int)response.StatusCode, response);
            }

            return Ok(response);

        }

    }
}
