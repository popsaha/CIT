using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/Branch")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        public readonly IBranchRepositoty _Ibranchrepositoty;
        protected APIResponse _response;
        public BranchController(IBranchRepositoty branchRepositoty)
        {
            _Ibranchrepositoty = branchRepositoty;
        }

        [HttpGet("GetAllBranchAPI")]
        public async Task<IActionResult> GetAllbranch()
        {
            try
            {
                IEnumerable<BranchMaster> branchModels;

                branchModels = await _Ibranchrepositoty.GetAllBranch();

                return Ok(branchModels);
            }

            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpPost("AddbranchAPI")]
        public async Task<IActionResult> Addbranch(BranchDTO branchDTO)
        {
            int Res = 0;
            try
            {
                if (ModelState.IsValid)
                {

                    Res = await _Ibranchrepositoty.AddBranch(branchDTO);

                }
                return Ok(Res);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }

        }

        [HttpGet("GetBranchAPI")]
        public async Task<IActionResult> GetBranch([FromRoute(Name = "branchId")] int branchId)
        {
            BranchMaster branchModel = new BranchMaster();
            try
            {
                branchModel = await _Ibranchrepositoty.GetBranch(branchId);
                return Ok(branchModel);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }

        }

        [HttpPut("UpdateBranchAPI")]
        public async Task<IActionResult> UpdateBranch(BranchDTO branchRequestModel)
        {
            int Res = 0;
            try
            {
                Res = await _Ibranchrepositoty.UpdateBranch(branchRequestModel);
                return Ok(Res);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message, ex.StackTrace);
            }
        }

        [HttpDelete("DeleteBranchAPI")]
        public async Task<IActionResult> DeleteBranch(int branchId, int deletedBy)
        {
            int Res = 0;
            try
            {
                Res = await _Ibranchrepositoty.DeleteBranch(branchId, deletedBy);
                return Ok("The user is Deleted");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);

            }
        }
    }
}
