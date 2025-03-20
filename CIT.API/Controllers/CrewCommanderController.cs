using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.CrewCommander;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/CrewCommander")]
    [ApiController]
    public class CrewCommanderController : Controller
    {
        protected readonly ICrewCommanderRepository _crewCommander;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public CrewCommanderController(ICrewCommanderRepository crewCommander, IMapper mapper)
        {
            _crewCommander = crewCommander;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetCrewCommander()
        {
            try
            {
                IEnumerable<UserMaster> crewCommanders = await _crewCommander.GetAllCrewCommanderList();

                if (crewCommanders == null || !crewCommanders.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Task List found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<CrewCommanderDTO>>(crewCommanders);

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
