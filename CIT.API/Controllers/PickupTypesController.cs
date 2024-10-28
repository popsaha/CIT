using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.PickupType;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/PickupType")]
    [ApiController]
    public class PickupTypesController : ControllerBase
    {
        private readonly IPickupTypesRepository _pickupTypes;
        private readonly APIResponse _response;
        private readonly IMapper _mapper;


        public PickupTypesController(IPickupTypesRepository pickupTypes, IMapper mapper)
        {
            _pickupTypes = pickupTypes;
            _mapper = mapper;
            _response = new ();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]

        public async Task<ActionResult<APIResponse>> GetAllPickupType()
        {
            try
            {
                IEnumerable<PickupTypes> types = await _pickupTypes.GetAllPickupTypesAsync();

                if(types == null || !types.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No PickupType found");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result= _mapper.Map<List<PickupTypesDTO>>(types);
                return Ok(_response);
            }
            catch (Exception ex) {

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);

                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
