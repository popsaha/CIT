﻿using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using CIT.API.Models;
using CIT.API.Models.Dto.Order;

namespace CIT.API.Repository
{
    public class CrewTaskDetailsRepository : ICrewTaskDetailsRepository
    {
        private readonly DapperContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CrewTaskDetailsRepository(DapperContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        // Static method to retrieve userId from uuid
        public static async Task<int> GetUserIdFromUuidAsync(Guid uuid, DapperContext db)
        {
            using (var con = db.CreateConnection())
            {
                var query = "SELECT UserId FROM UserMaster WHERE Uuid = @Uuid";
                return await con.QueryFirstOrDefaultAsync<int>(query, new { Uuid = uuid });
            }
        }

        // New method to call the static method
        public async Task<int> GetUserIdByUuidAsync()
        {
            // Extract UUID from JWT claims
            var uuidClaim = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == "uuid")?.Value;

            if (string.IsNullOrEmpty(uuidClaim) || !Guid.TryParse(uuidClaim, out var uuid))
            {
                throw new UnauthorizedAccessException("Invalid or missing UUID in token.");
            }

            // Use the static method to get the UserId
            return await GetUserIdFromUuidAsync(uuid, _db);
        }


        // New method to get tasks for a specific Crew Commander
        public async Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId, int userId, DateTime? orderDate = null)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'A'); // Use flag 'A' for getting all tasks
                parameters.Add("CrewCommanderId", crewCommanderId); // Pass the crew commander ID
                parameters.Add("UserId", userId);

                if (orderDate.HasValue)
                    parameters.Add("OrderDate", orderDate.Value);

                var crewTasks = await con.QueryAsync<CrewTaskDetailsDTO>(
                    "spCrewTaskDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return crewTasks.ToList();
            }
        }

        public async Task<CrewTaskDetailsByTaskIdDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId, int taskId, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'B'); // Use flag 'B' for getting task details by TaskId
                parameters.Add("CrewCommanderId", crewCommanderId); // Ensure this crew commander is authorized for this task
                parameters.Add("TaskId", taskId); // Pass TaskId to get specific task details
                parameters.Add("UserId", userId);


                // Use QueryFirstOrDefaultAsync to ensure that the query returns null if no record is found
                var taskDetails = await con.QueryFirstOrDefaultAsync<CrewTaskDetailsByTaskIdDTO>(
                    "spCrewTaskDetails", // This stored procedure must ensure that crewCommanderId is checked
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return taskDetails; // Returns null if task isn't found or crew commander isn't authorized
            }
        }


        public async Task<string> GetNextScreenIdByTaskId(int taskId)
        {
            // Get the current ScreenId
            var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);

            // If current ScreenId is null, initialize to default
            if (string.IsNullOrEmpty(currentScreenId))
            {
                return "CIT-2";
            }

            // Check if the ScreenId follows the format "CIT-<number>"
            if (currentScreenId.StartsWith("CIT-"))
            {
                // Extract the numeric part of the ScreenId
                var screenNumberPart = currentScreenId.Substring(4);

                if (int.TryParse(screenNumberPart, out int currentNumber))
                {
                    // Increment the numeric part to get the next screen ID
                    int nextNumber = currentNumber + 1;
                    return $"CIT-{nextNumber}";
                }
            }

            // Default value if ScreenId is in an unexpected format
            return "CIT-2";
        }


        public async Task<string> GetCurrentScreenIdByTaskId(int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                var query = "SELECT NextScreenId FROM Task WHERE TaskID = @TaskId";
                var screenId = await con.QueryFirstOrDefaultAsync<string>(query, new { TaskId = taskId });
                if (screenId == null)
                {
                    Console.WriteLine($"NextScreenId could not be found for TaskID: {taskId}");
                }
                return screenId;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO updateDTO, string activityType, int userId)
        {

            //var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
            //if (currentScreenId == "1")
            //{
            //    return false; // Prevent update if task is already marked as failed
            //}


            using (var con = _db.CreateConnection())
            {
                // Check if current ScreenId is already CIT-6 before proceeding
                //var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                //if (currentScreenId == "CIT-6")
                //{
                //    return false; // Prevent update if task is marked as completed
                //}

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'C');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", userId);
                // Set ScreenId based on specific activity types
                //int screenId = activityType switch
                //{
                //    "Arrived" => 2,                                      
                //    "Completed" => 7,
                //    _ => 1 // Default screenId for other activity types
                //};

                parameters.Add("NextScreenId", updateDTO.NextScreenId); // Set ScreenId based on activityType  // Set ScreenId to 1 as required by the update
                parameters.Add("Time", updateDTO.Time);  // Pass the start time from DTO
                parameters.Add("Lat", updateDTO.Location?.Lat);  // Pass Latitude if available
                parameters.Add("Long", updateDTO.Location?.Long);  // Pass Longitude if available
                parameters.Add("ActivityType", activityType);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }


        public async Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskParcelDTO parcelDTO, string activityType, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                // Check if current ScreenId is already CIT-6 before proceeding
                //var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                //if (currentScreenId == "CIT-6")
                //{
                //    return false; // Prevent update if task is marked as completed
                //}

                // Check for duplicate ParcelQR values
                var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                if (parcelQRs.Count != parcelQRs.Distinct().Count())
                {
                    Console.WriteLine("Duplicate Parcel QR codes detected.");
                    return false; // Duplicate ParcelQR codes detected
                }

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'D');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", userId);
                parameters.Add("NextScreenId", parcelDTO.NextScreenId);
                parameters.Add("Time", parcelDTO.Time);
                parameters.Add("Lat", parcelDTO.Location?.Lat);
                parameters.Add("Long", parcelDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);

                // Create a comma-separated string from unique ParcelQR values
                var parcelsCsv = string.Join(",", parcelQRs);
                parameters.Add("ParcelsLoaded", parcelsCsv);
                parameters.Add("ParcelsUnloaded", parcelsCsv);

                var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                parameters.Add("ParcelsUnloaded", parcelsCsv2);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }


        public async Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, CrewTaskFailedStatusDTO failedDTO, string activityType, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                if (currentScreenId == "-1")
                {
                    return false; // Prevent update if task is already marked as failed
                }

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'E');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", userId);
                parameters.Add("NextScreenId", failedDTO.NextScreenId);
                parameters.Add("Time", failedDTO.Time);
                parameters.Add("Lat", failedDTO.Location?.Lat);
                parameters.Add("Long", failedDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);
                parameters.Add("FailureReason", failedDTO.FailureReason);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }

        public async Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId)
        {
            using (var con = _db.CreateConnection())
            {
                // Check if current ScreenId is already CIT-6 before proceeding
                //var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                //if (currentScreenId == "CIT-6")
                //{
                //    return false; // Prevent update if task is marked as completed
                //}

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", 'F');
                parameters.Add("CrewCommanderId", crewCommanderId);
                parameters.Add("TaskId", taskId);
                parameters.Add("Status", status);
                parameters.Add("UserId", userId);

                //int screenId = activityType == "ArrivedDelivery" ? 5 : 4;
                parameters.Add("NextScreenId", arrivedDTO.NextScreenId);
                parameters.Add("Time", arrivedDTO.Time);
                parameters.Add("Lat", arrivedDTO.Location?.Lat);
                parameters.Add("Long", arrivedDTO.Location?.Long);
                parameters.Add("ActivityType", activityType);

                var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
                return result > 0;
            }
        }

        // Method to fetch parcel data as comma-separated values

        public async Task<IEnumerable<Parcel>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb)
        {
            const string query = @"
    SELECT ctd.ParcelsLoaded
    FROM CitTaskDetail ctd
    INNER JOIN Task t ON ctd.TaskID = t.TaskID
    INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
    INNER JOIN UserMaster u ON ta.CrewID = u.UserID
    WHERE ctd.TaskID = @TaskId
      AND ta.CrewID = @CrewCommanderId
      AND ta.IsActive = 1
      AND u.UserID = @CrewCommanderId";

            using (var connection = _db.CreateConnection())
            {
                // Validate the crew commander is authorized to access the task
                var parcelsLoadedStrings = await connection.QueryAsync<string>(
                    query,
                    new
                    {
                        TaskId = taskId,
                        CrewCommanderId = authenticatedUserId
                    });

                // If no rows are returned, the user isn't authorized or the task doesn't exist
                if (!parcelsLoadedStrings.Any())
                {
                    throw new UnauthorizedAccessException("User is not authorized to access this task or task ID is invalid.");
                }

                // Process the parcel data
                return parcelsLoadedStrings
                    .Where(row => !string.IsNullOrWhiteSpace(row)) // Exclude NULL or empty rows
                    .SelectMany(row => row.Split(',', StringSplitOptions.RemoveEmptyEntries)) // Split by comma
                    .Select(parcelQr => new Parcel { ParcelQR = parcelQr.Trim() }); // Map to Parcel objects
            }
        }

    }
}
