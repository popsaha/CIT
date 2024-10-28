using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace CIT.API.Repository
{
    public class CrewTaskDetailsRepository : ICrewTaskDetailsRepository
    {
        private readonly DapperContext _db;
        private readonly IMapper _mapper;

        public CrewTaskDetailsRepository(DapperContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
        // New method to get tasks for a specific Crew Commander
        public async Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'A'); // Use flag 'A' for getting all tasks
                parameters.Add("CrewCommanderId", crewCommanderId); // Pass the crew commander ID

                var crewTasks = await con.QueryAsync<CrewTaskDetailsDTO>(
                    "spCrewTaskDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return crewTasks.ToList();
            }
        }

        public async Task<CrewTaskDetailsDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId, int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'B'); // Use flag 'B' for getting task details by TaskId
                parameters.Add("CrewCommanderId", crewCommanderId); // Ensure this crew commander is authorized for this task
                parameters.Add("TaskId", taskId); // Pass TaskId to get specific task details

                // Use QueryFirstOrDefaultAsync to ensure that the query returns null if no record is found
                var taskDetails = await con.QueryFirstOrDefaultAsync<CrewTaskDetailsDTO>(
                    "spCrewTaskDetails", // This stored procedure must ensure that crewCommanderId is checked
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return taskDetails; // Returns null if task isn't found or crew commander isn't authorized
            }
        }
        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'C'); // Set the flag to 'C' for updating the task status
                parameters.Add("CrewCommanderId", crewCommanderId); // Add CrewCommanderId
                parameters.Add("TaskId", taskId); // The ID of the task to be updated
                parameters.Add("Status", status); // The new status (e.g., "In Process")

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
                return result > 0; // Return true if the update was successful
            }
        }

        

    }
}
