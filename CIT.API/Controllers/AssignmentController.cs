using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.OrderAssignment;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Assignment")]
    [ApiController]
    public class AssignmentController : ControllerBase
    {
        public readonly IOrderAssignmentRepository _orderAssignmentRepo;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchController> _logger;

        public AssignmentController(IOrderAssignmentRepository orderAssignmentRepo, IMapper mapper, ILogger<BranchController> logger)
        {
            _orderAssignmentRepo = orderAssignmentRepo;
            _mapper = mapper;
            _logger = logger;
            _response = new();
        }

        [HttpPost("assign")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> AssignTeams([FromBody] AssignmentRequestDto request)
        {
            try
            {
                var result = await _orderAssignmentRepo.AssignTeamsRandomly(
                    request.AssignDate,
                    request.CrewId,
                    request.LeadVehicleId,
                    request.ChaseVehicleId
                );
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning teams");

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _response.StatusCode = HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
