using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.OrderType;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/OrderType")]
    [ApiController]
    public class OrderTypeController : ControllerBase
    {
        private readonly IOrderTypeRepository _orderTypeRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        public OrderTypeController(IOrderTypeRepository IorderTypeRepository, IMapper mapper)
        {
            _orderTypeRepository = IorderTypeRepository;
            _mapper = mapper;
            _response = new();
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllOrderType()
        {
            try
            {
                IEnumerable<OrderTypeMaster> ordertypeList = await _orderTypeRepository.GetAllOrderType();
                if (ordertypeList == null || !ordertypeList.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Order Type found.");
                    return NotFound(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<OrderTypeDTO>>(ordertypeList);

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);

                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

        }

        [HttpGet("{OrderTypeID:int}", Name = "GetOrderType")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetOrderType([FromRoute(Name = "OrderTypeID")] int OrderTypeID)
        {
            OrderTypeMaster orderTypeMaster = new OrderTypeMaster();
            try
            {
                if (OrderTypeID == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                orderTypeMaster = await _orderTypeRepository.GetOrderType(OrderTypeID);
                if (orderTypeMaster == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                //_response.Result = customer;
                _response.Result = _mapper.Map<OrderTypeDTO>(orderTypeMaster);
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> AddOrderType(OrderTypeCreateDTO orderTypecreateDTO)
        {
            int Res = 0;
            try
            {
                if (orderTypecreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Order Type data");
                    return BadRequest(_response);
                }
                var orderTypeMaster = _mapper.Map<OrderTypeMaster>(orderTypecreateDTO);

                Res = await _orderTypeRepository.AddOrderType(orderTypecreateDTO);

                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Order type already exists.");
                    return BadRequest(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.NoContent;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
            return _response;
        }



        [HttpPut("{OrderTypeID:int}", Name = "UpdateOrderType")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]    
        public async Task<ActionResult<APIResponse>> UpdateOrderType(int OrderTypeID, OrderTypeUpdateDTO orderTypeupdateDTO)
        {
            int Res = 0;
            try
            {
                if (orderTypeupdateDTO == null || OrderTypeID != orderTypeupdateDTO.OrderTypeID)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid order type Id.");

                    return BadRequest(_response);
                }
                OrderTypeMaster ordertypemaster = _mapper.Map<OrderTypeMaster>(orderTypeupdateDTO);

                Res = await _orderTypeRepository.UpdateOrderType(ordertypemaster);
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

        [HttpDelete("{OrderTypeID:int}", Name = "DeleteOrderType")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteOrderType(int OrderTypeID, int deletedBy)
        {
            int Res = 0;
            try
            {
                if (OrderTypeID == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Order type Id.");
                    return BadRequest(_response);
                }

                Res = await _orderTypeRepository.DeleteOrderType(OrderTypeID, deletedBy);
                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Order Type not found.");
                    return NotFound(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.NoContent;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };

            }
            return _response;
        }
    }
}
