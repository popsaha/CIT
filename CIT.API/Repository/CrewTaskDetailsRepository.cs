using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using CIT.API.Models;

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
        public async Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'A'); // Use flag 'A' for getting all tasks
                parameters.Add("CrewCommanderId", crewCommanderId); // Pass the crew commander ID
                parameters.Add("UserId", userId);

                var crewTasks = await con.QueryAsync<CrewTaskDetailsDTO>(
                    "spCrewTaskDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return crewTasks.ToList();
            }
        }

        public async Task<CrewTaskDetailsDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId, int taskId, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'B'); // Use flag 'B' for getting task details by TaskId
                parameters.Add("CrewCommanderId", crewCommanderId); // Ensure this crew commander is authorized for this task
                parameters.Add("TaskId", taskId); // Pass TaskId to get specific task details
                parameters.Add("UserId", userId);

                // Use QueryFirstOrDefaultAsync to ensure that the query returns null if no record is found
                var taskDetails = await con.QueryFirstOrDefaultAsync<CrewTaskDetailsDTO>(
                    "spCrewTaskDetails", // This stored procedure must ensure that crewCommanderId is checked
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return taskDetails; // Returns null if task isn't found or crew commander isn't authorized
            }
        }


        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO updateDTO, string activityType)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'C');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", updateDTO.UserId);
                // Set ScreenId based on specific activity types
                int screenId = activityType switch
                {
                    "Arrived" => 2,                  
                    "Unloaded" => 6,
                    "Completed" => 7,
                    _ => 1 // Default screenId for other activity types
                };

                parameters.Add("ScreenId", screenId); // Set ScreenId based on activityType  // Set ScreenId to 1 as required by the update
                parameters.Add("Time", updateDTO.Time);  // Pass the start time from DTO
                parameters.Add("Lat", updateDTO.Location?.Lat);  // Pass Latitude if available
                parameters.Add("Long", updateDTO.Location?.Long);  // Pass Longitude if available
                parameters.Add("ActivityType", activityType);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }


        public async Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskParcelDTO parcelDTO, string activityType)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'D');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", parcelDTO.UserId);
                parameters.Add("ScreenId", 4);
                parameters.Add("Time", parcelDTO.Time);
                parameters.Add("Lat", parcelDTO.Location?.Lat);
                parameters.Add("Long", parcelDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);

                // Create a comma-separated string from ParcelQR values
                var parcelsCsv = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                parameters.Add("ParcelsLoaded", parcelsCsv);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }

        public async Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, CrewTaskFailedStatusDTO failedDTO, string activityType)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'E');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", failedDTO.UserId);
                parameters.Add("ScreenId", 3);
                parameters.Add("Time", failedDTO.Time);
                parameters.Add("Lat", failedDTO.Location?.Lat);
                parameters.Add("Long", failedDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);
                parameters.Add("FailureReason", failedDTO.FailureReason);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }

        public async Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO arrivedDTO, string activityType)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'F');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", arrivedDTO.UserId);

                int screenId = activityType == "ArrivedDelivery" ? 5 : 4;
                parameters.Add("ScreenId", screenId);
                parameters.Add("Time", arrivedDTO.Time);
                parameters.Add("Lat", arrivedDTO.Location?.Lat);
                parameters.Add("Long", arrivedDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
                return result > 0;
            }
        }

        // Method to fetch parcel data as comma-separated values
        public async Task<string> GetParcelData(int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                var query = "SELECT ParcelsLoaded  FROM CITTASKDETAIL WHERE TaskId = @TaskId";
                return await con.QueryFirstOrDefaultAsync<string>(query, new { TaskId = taskId });
            }
        }

    }
}
