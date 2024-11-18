using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.Role;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Role")]
    [ApiController]
    public class RoleMasterController : ControllerBase
    {
        public readonly IRoleRepository _roleRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<RoleMasterController> _logger;

        public RoleMasterController(IRoleRepository roleRepository, IMapper mapper, ILogger<RoleMasterController> logger)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
            _logger = logger;
            _response = new();
        }


        [HttpGet("GetAllRole")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllRole()
        {
            _logger.LogInformation("GetAllRole method called at {time}.", DateTime.UtcNow);
            try
            {
                IEnumerable<RoleMaster> roleModels = await _roleRepository.GetAllRole();

                if (roleModels == null || !roleModels.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Role found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<RoleListDTO>>(roleModels);

                return Ok(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Role at {time}.", DateTime.UtcNow);

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
