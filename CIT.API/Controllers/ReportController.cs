using Azure;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using AutoMapper;
using CIT.API.Repository.IRepository;


namespace CIT.API.Controllers
{
    public class ReportController : ControllerBase
    {
        public readonly IReport _IReport;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchController> _logger;

        public ReportController(IReport report, IMapper mapper, ILogger<BranchController> logger)
        {
            _IReport = report;
            _mapper = mapper;
            _logger = logger;
            _response = new();
        }


        [HttpGet("GetReportsData")]

        public async Task<ActionResult<ReportDetails>> GetReportsData()
        {
            _logger.LogInformation("GetReportsData method called at {time}.", DateTime.UtcNow);
            IEnumerable<ReportDetails> ReportModels;

            try
            {
                 ReportModels = await _IReport.GetAllReportData();


                //if (ReportModels == null || !ReportModels.Any())
                //{
                //    _response.StatusCode = HttpStatusCode.NotFound;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages.Add("No Data found.");
                //    return NotFound(_response);
                //}
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<ReportDetails>>(ReportModels);

                return Ok(_response);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Reports at {time}.", DateTime.UtcNow);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

           // return ReportModels;
        }

        [HttpPost("GetFilterReportsData")]
        public async Task<ActionResult<ReportDetails>> GetFilterReportsData([FromBody] ReportDetailsParam _ReportDetailsParam)
        {
            _logger.LogInformation("GetFilterReportsData method called at {time}.", DateTime.UtcNow);
            IEnumerable<ReportDetails> ReportModels;

            try
            {
                ReportModels = await _IReport.GetFilteredReportData(_ReportDetailsParam);
                //if (ReportModels == null || !ReportModels.Any())
                //{
                //    _response.StatusCode = HttpStatusCode.NotFound;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages.Add("No Data found.");
                //    return NotFound(_response);
                //}
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<ReportDetails>>(ReportModels);

                return Ok(_response);

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GetFilterReportsData() at {time}.", DateTime.UtcNow);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

           //return ReportModels;
        }
    }
}
