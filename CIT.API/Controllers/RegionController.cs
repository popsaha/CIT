using CIT.API.Models.Dto;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/Region")]
    [ApiController]
    public class RegionController : ControllerBase
    {
        private readonly IRegionRepository _regionRepository;
        public RegionController(IRegionRepository regionRepository)
        {
            _regionRepository = regionRepository;
        }
        [HttpGet("GetRegionAPI")]
        public async Task<IActionResult> GetallRegion()
        {
            var customers = await _regionRepository.GetallRegion();
            return Ok(customers);
        }

        [HttpPost("AddRegionAPI")]
        public async Task<IActionResult> AddRegion(RegionDTO regionDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {
                    Res = await _regionRepository.AddRegion(regionDTO);
                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }

        }

        [HttpGet("GetCustomerAPI")]
        public async Task<IActionResult> GetCustomer([FromRoute(Name = "RegionID")] int RegionID)
        {
            RegionMaster regionMaster = new RegionMaster();
            try
            {
                regionMaster = await _regionRepository.GetRegion(RegionID);
                return Ok(regionMaster);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPut("UpdateRegionAPI")]
        public async Task<IActionResult> UpdateRegion(RegionDTO regionDTO)
        {
            int Res = 0;
            try
            {
                Res = await _regionRepository.UpdateRegion(regionDTO);
                return Ok(Res);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpDelete("DeleteRegionAPI")]
        public async Task<IActionResult> DeleteRegion(int RegionID, int deletedBy)
        {
            int Res = 0;
            try
            {
                Res = await _regionRepository.DeleteRegion(RegionID, deletedBy);
                return Ok("The user is Deleted");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);

            }
        }
    }
}
