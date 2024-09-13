using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Customer;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CIT.API.Controllers
{
    [Route("api/Region")]
    [ApiController]
    public class RegionController : ControllerBase
    {
        private readonly IRegionRepository _regionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        public RegionController(IRegionRepository regionRepository, IMapper mapper)
        {
            _regionRepository = regionRepository;
            _mapper = mapper;
            _response = new();
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetallRegion()
        {
            try
            {
                IEnumerable<RegionMaster> RegionList = await _regionRepository.GetallRegion();
                if (RegionList == null || !RegionList.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("No Region found.");
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<RegionDTO>>(RegionList);

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);

                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            //return Ok(customers);
        }

        [HttpGet("{RegionID:int}", Name = "GetRegion")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetRegion(int RegionID)
        {
            RegionMaster regionMaster = new RegionMaster();
            try
            {
                if (RegionID == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                regionMaster = await _regionRepository.GetRegion(RegionID);
                if (regionMaster == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<RegionDTO>(regionMaster);
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
        public async Task<ActionResult<APIResponse>> AddRegion(RegionDTO regionDTO)
        {
            int Res = 0;
            try
            {
                if (regionDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Region data");
                    return BadRequest(_response);
                }
                var region = _mapper.Map<RegionMaster>(regionDTO);

                Res = await _regionRepository.AddRegion(regionDTO);

                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Region already exists.");
                    return BadRequest(_response);
                }
                if (Res > 0)
                {
                    _response.StatusCode = HttpStatusCode.Created;
                    _response.IsSuccess = true;
                    _response.Result = region;
                    //return CreatedAtRoute("GetRegion", new { branchId = region.RegionID }, _response);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
            return _response;

        }

        [HttpPut("{RegionID:int}", Name = "UpdateRegion")]
        //[Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateRegion(int RegionID, RegionDTO regionDTO)
        {
            int Res = 0;
            try
            {
                if (regionDTO == null || RegionID != regionDTO.RegionID)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid region Id.");
                    return BadRequest(_response);
                }
                RegionMaster region = _mapper.Map<RegionMaster>(regionDTO);
                Res = await _regionRepository.UpdateRegion(region);
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


        [HttpDelete("{RegionID:int}", Name = "DeleteRegion")]
       //Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> DeleteRegion(int RegionID, int deletedBy)
        {
            int Res = 0;
            try
            {
                if (RegionID == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid Region Id.");
                    return BadRequest(_response);
                }
                Res = await _regionRepository.DeleteRegion(RegionID, deletedBy);
                if (Res == 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Region not found");
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
