using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.ChaseVehicle;
using CIT.API.Models.Dto.Vehicle;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace CIT.API.Controllers
{
    [Route("api/ChaseVehicle")]
    [ApiController]
    public class ChaseVehicleController : ControllerBase
    {
        private readonly IChaseVehicleRepository _chaseVehicleRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public ChaseVehicleController(IChaseVehicleRepository chaseVehicleRepository, IMapper mapper)
        {
            _chaseVehicleRepository = chaseVehicleRepository;
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
                IEnumerable<ChaseVehicle> vehicles = await _chaseVehicleRepository.GetAllVehicle();

                if (vehicles == null || !vehicles.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Task List found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<ChaseVehicleDTO>>(vehicles);

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

        [HttpGet("{id:int}", Name = "GetChaseVehicle")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetVehicleById(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var vehicle = await _chaseVehicleRepository.GetVehicleById(id);

                if (vehicle == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                //_response.Result = customer;
                _response.Result = _mapper.Map<ChaseVehicleDTO>(vehicle);
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
        public async Task<ActionResult<APIResponse>> CreateVehicle([FromBody] ChaseVehicleCreateDTO createDTO)
        {
            int Res = 0;
            try
            {

                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Vehicle data.");
                    return BadRequest(_response);
                }

                // Get the userId from the claims (JWT token)
                var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var vehicleData = _mapper.Map<ChaseVehicle>(createDTO);

                Res = await _chaseVehicleRepository.AddVehicle(createDTO, userId);

                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Chse Vehicle already exists.");
                    return BadRequest(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.Created;
                    _response.IsSuccess = true;
                    _response.Result = vehicleData;
                    //return CreatedAtRoute("GetPolice", new { policeId = police.PoliceId }, _response);
                }
                // Return the created police with the location of the new resource              
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;
        }


        [HttpPut("{id:int}", Name = "UpdateChaseVehicle")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateVehicle(int id, [FromBody] ChaseVehicleUpdateDTO updateDTO)
        {

            try
            {
                if (updateDTO == null || id != updateDTO.VehicleID)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Vehicle Id.");

                    return BadRequest(_response);
                }

                ChaseVehicle vehicleUpdate = _mapper.Map<ChaseVehicle>(updateDTO);
                var updatedVehicle = await _chaseVehicleRepository.UpdateVehicle(vehicleUpdate);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = updatedVehicle;
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


        [HttpDelete("{vehicleId:int}", Name = "DeleteChaseVehicle")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteVehicle(int vehicleId, int userId)
        {
            try
            {
                if (vehicleId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Vehicle Id.");

                    return BadRequest(_response);
                }

                var police = await _chaseVehicleRepository.GetVehicleById(vehicleId);

                if (police == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Vehicle not found.");

                    return NotFound(_response);
                }

                var deletedVehicle = await _chaseVehicleRepository.DeleteVehicle(vehicleId, userId);

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

    }
}
