﻿using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Customer;
using CIT.API.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
        public BranchController(IBranchRepositoty branchRepositoty, IMapper mapper)
        {
            _Ibranchrepositoty = branchRepositoty;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet("GetAllBranchAPI")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetAllbranch()
        {
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
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetBranchAPI")]
        [HttpGet("{branchId:int}", Name = "GetBranch")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBranch(int branchId)
        {
            BranchMaster branchModel = new BranchMaster();
            try
            {
                if (branchId == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                branchModel = await _Ibranchrepositoty.GetBranch(branchId);

                if (branchModel == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
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
        public async Task<ActionResult<APIResponse>> Addbranch([FromBody] BranchDTO branchDTO)
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
                    _response.StatusCode = HttpStatusCode.NoContent;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, ex.StackTrace);
            }
            return _response;
        }

        [HttpPut("{BranchID:int}", Name = "UpdateBranch")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateBranch(int BranchID, BranchDTO branchRequestModel)
        {
            int Res = 0;
            try
            {
                if (branchRequestModel == null || BranchID != branchRequestModel.BranchID)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Invalid customer Id.");

                    return BadRequest(_response);
                }
                BranchMaster branch = _mapper.Map<BranchMaster>(branchRequestModel);
                Res = await _Ibranchrepositoty.UpdateBranch(branch);
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

        [HttpDelete("{branchId:int}", Name = "DeleteBranch")]
        [Authorize(Roles = "admin")]
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
