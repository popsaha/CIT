using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto.Branch;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Branch")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        public readonly IBranchRepositoty _Ibranchrepositoty;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchController> _logger;

        public BranchController(IBranchRepositoty branchRepositoty, IMapper mapper, ILogger<BranchController> logger)
        {
            _Ibranchrepositoty = branchRepositoty;
            _mapper = mapper;
            _logger = logger;
            _response = new();
        }

        [HttpGet("GetAllbranch")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllbranch()
        {
            _logger.LogInformation("GetAllbranch method called at {time}.", DateTime.UtcNow);

            try
            {
                IEnumerable<BranchMaster> branchModels = await _Ibranchrepositoty.GetAllBranch();

                if (branchModels == null || !branchModels.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Branch found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<BranchDTO>>(branchModels);

                return Ok(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches at {time}.", DateTime.UtcNow);

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        //[HttpGet("{branchId:int}", Name = "GetBranch")]
        [HttpGet("GetBranch")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetBranch(int branchId)
        {
            BranchMaster branchModel = new BranchMaster();
            try
            {
                if (branchId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages.Add("Invalid branch id passed");
                    return BadRequest(_response);
                }
                branchModel = await _Ibranchrepositoty.GetBranch(branchId);

                if (branchModel == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Data not found");
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<BranchDTO>(branchModel);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }

        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> Addbranch([FromBody] BranchCreateDTO branchDTO)
        {
            int Res = 0;
            try
            {
                if (branchDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Branch data");
                    return BadRequest(_response);
                }
                var branch = _mapper.Map<BranchMaster>(branchDTO);

                Res = await _Ibranchrepositoty.AddBranch(branchDTO);
                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Branch already exists.");
                    return BadRequest(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.Created;
                    _response.IsSuccess = true;
                    _response.Result = branch;

                    // Return the created customer with the location of the new resource
                    return CreatedAtRoute("GetBranch", new { branchId = Res }, _response);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
            return _response;
        }

        [HttpPut("{BranchID:int}", Name = "UpdateBranch")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateBranch(int BranchID, [FromBody] BranchUpdateDTO branchdto)
        {
            int Res = 0;
            try
            {
                if (branchdto == null || BranchID != branchdto.BranchID)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid customer Id.");

                    return BadRequest(_response);
                }
                BranchMaster branchmaster = _mapper.Map<BranchMaster>(branchdto);

                Res = await _Ibranchrepositoty.UpdateBranch(branchmaster);

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

        [HttpDelete("DeleteBranch")]
        // [HttpDelete("{branchId:int}", Name = "DeleteBranch")]     
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteBranch(int branchId, int deletedBy)
        {
            int Res = 0;
            try
            {

                if (branchId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Branch Id.");
                    return BadRequest(_response);
                }

                Res = await _Ibranchrepositoty.DeleteBranch(branchId, deletedBy);
                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Branch not found.");
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
