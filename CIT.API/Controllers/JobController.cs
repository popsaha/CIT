using CIT.API.Repository.IRepository;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace CIT.API.Controllers
{
    [Route("api/Job")]
    [ApiController]
    public class JobController : ControllerBase
    {

        private readonly IJobRepository _jobRepository;
        //private readonly IBackgroundJobClient backgroundJobCLient;
        private readonly IRecurringJobManager _recurringJobManager;

        public JobController(IJobRepository jobRepository, IRecurringJobManager recurringJobManager)
        {
            _jobRepository = jobRepository;
            //_backgroundJobCLient = backgroundJobCLient;
            _recurringJobManager = recurringJobManager;
        }

        [HttpPost("schedule-recurring-job")]
        public IActionResult CreateRecurringJob()
        {
            // Schedule a recurring job to run daily at 12:00 AM
            _recurringJobManager.AddOrUpdate(
                "GenerateRecurringOrders", //Job ID
                () => _jobRepository.GenerateRecurringOrdersAsync(),
                Cron.Daily(0)
            );

            return Ok("Recurring order job scheduled to run daily at 12:00 AM.");
        }
    }
}
