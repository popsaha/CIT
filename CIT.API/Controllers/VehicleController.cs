using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Vehicle;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Vehicle")]
    [ApiController]
    public class VehicleController : Controller
    {
        private readonly IVehicleRepository _vehicleRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public VehicleController( IVehicleRepository vehicleRepository ,IMapper mapper)
        {
            _vehicleRepository = vehicleRepository;
            _mapper = mapper;
            _response = new();
        }



        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllVehicleList()
        {
            try
            {
                IEnumerable<Vehicle> vehicles = await _vehicleRepository.GetAllVehicle();

                if (vehicles == null || !vehicles.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Task List found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<VehicleDTO>>(vehicles);

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
    }
}
