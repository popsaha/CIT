﻿using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto.AtmCrewTaskDetails;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class AtmCrewTaskDetailsRepository : IAtmCrewTaskDetailsRepository
    {

        private readonly DapperContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AtmCrewTaskDetailsRepository> _logger;
        public AtmCrewTaskDetailsRepository(DapperContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor, ILogger<AtmCrewTaskDetailsRepository> logger)
        {
            _db = db;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Static method to retrieve userId from uuid
        public static async Task<int> GetUserIdFromUuidAsync(Guid uuid, DapperContext db)
        {
            try
            {
                using (var con = db.CreateConnection())
                {
                    var query = "SELECT UserId FROM UserMaster WHERE Uuid = @Uuid";
                    return await con.QueryFirstOrDefaultAsync<int>(query, new { Uuid = uuid });
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        public async Task<int> GetUserIdByUuidAsync()
        {
            try
            {
                // Extract UUID from JWT claims
                var uuidClaim = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == "uuid")?.Value;

                if (string.IsNullOrEmpty(uuidClaim) || !Guid.TryParse(uuidClaim, out var uuid))
                {
                    _logger.LogWarning("Invalid or missing UUID in token.");
                    throw new UnauthorizedAccessException("Invalid or missing UUID in token.");
                }
                _logger.LogInformation("Extracted UUID from token: {Uuid}", uuid);
                // Use the static method to get the UserId
                return await GetUserIdFromUuidAsync(uuid, _db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while extracting UserId from UUID.");
                throw;
            }
        }

        public async Task<string> GetCurrentScreenIdByTaskId(int taskId)
        {
            try
            {
                _logger.LogInformation("Fetching CurrentScreenId for TaskID: {TaskId}", taskId);
                using (var con = _db.CreateConnection())
                {
                    var query = "SELECT NextScreenId FROM Task WHERE TaskID = @TaskId";
                    var screenId = await con.QueryFirstOrDefaultAsync<string>(query, new { TaskId = taskId });
                    if (screenId == null)
                    {
                        _logger.LogWarning("NextScreenId could not be found for TaskID: {TaskId}", taskId);
                        Console.WriteLine($"NextScreenId could not be found for TaskID: {taskId}");
                    }
                    _logger.LogInformation("Retrieved NextScreenId: {ScreenId} for TaskID: {TaskId}", screenId, taskId);
                    return screenId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching NextScreenId for TaskID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<string> GetNextScreenIdByTaskId(int taskId)
        {
            try
            {
                _logger.LogInformation("Fetching next ScreenId for TaskId: {TaskId}", taskId);

                var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                _logger.LogInformation("Current ScreenId for TaskId {TaskId}: {CurrentScreenId}", taskId, currentScreenId);

                // Mapping logic for ScreenId progression
                return currentScreenId switch
                {
                    //'CURRENT SCREEN' => 'NEXT SCREEN'
                    "ATM-Start" => "ATM-Arrived",
                    "ATM-Arrived" => "ATM-LoadedAtBank",
                    "ATM-LoadedAtBank" => "ATM-ArrivedDelivery",
                    "ATM-ArrivedDelivery" => "ATM-LoadedAtATM",
                    "ATM-LoadedAtATM" => "ATM-UnloadedAtAtm",
                    "ATM-UnloadedAtAtm" => "ATM-Completed",
                    "ATM-Completed" => "1",
                    /*_ => "-1"*/ // Default fallback
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching next ScreenId for TaskId: {TaskId}", taskId);
                throw;
            }
        }
        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, AtmCrewTaskStatusUpdateDTO updateDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Task Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);


                using (var con = _db.CreateConnection())
                {
                   
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'A');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);

                    parameters.Add("NextScreenId", updateDTO.NextScreenId); // Set ScreenId based on activityType  // Set ScreenId to 1 as required by the update
                    parameters.Add("Time", updateDTO.Time);  // Pass the start time from DTO
                    parameters.Add("Lat", updateDTO.Location?.Lat);  // Pass Latitude if available
                    parameters.Add("Long", updateDTO.Location?.Long);  // Pass Longitude if available
                    parameters.Add("ActivityType", activityType);

                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Task Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }

        public async Task<bool> ParcelLoadStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedDTO loadedDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("parcel load Task Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);


                using (var con = _db.CreateConnection())
                {

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'B');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);

                    parameters.Add("NextScreenId", loadedDTO.NextScreenId); // Set ScreenId based on activityType  // Set ScreenId to 1 as required by the update
                    parameters.Add("Time", loadedDTO.Time);  // Pass the start time from DTO
                    parameters.Add("PickupReceiptNumber", loadedDTO.PickupReceiptNumber);
                    parameters.Add("Lat", loadedDTO.Location?.Lat);  // Pass Latitude if available
                    parameters.Add("Long", loadedDTO.Location?.Long);  // Pass Longitude if available
                    parameters.Add("ParcelNumber", loadedDTO.ParcelNumber);
                    parameters.Add("ActivityType", activityType);

                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Task Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }

        public async Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, AtmCrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Arrived Delivery Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
                using (var con = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'C');
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

                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Arrived Delivery Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }
        public async Task<bool> ParcelLoadAtAtmStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedAtATMDTO cassetteDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel loaded at ATM Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
                using (var con = _db.CreateConnection())
                {

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'D');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);
                    parameters.Add("NextScreenId", cassetteDTO.NextScreenId);
                    parameters.Add("Time", cassetteDTO.Time);
                    parameters.Add("Lat", cassetteDTO.Location?.Lat);
                    parameters.Add("Long", cassetteDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("ParcelNumber", cassetteDTO.ParcelNumber);


                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Parcel loaded at atm Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }

        public async Task<IEnumerable<ParcelNo>> GetParcelLoadedAtBankAsync(int taskId, int authenticatedUserId)
        {
            try
            {
                _logger.LogInformation("Fetching ParcelLoadedAtBank for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);

                using (var connection = _db.CreateConnection())
                {
                    string query = @"
                      SELECT atd.ParcelLoadedAtBank
                      FROM AtmTaskDetail atd
                      INNER JOIN Task t ON atd.TaskID = t.TaskID
                      INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
                      INNER JOIN UserMaster u ON ta.CrewID = u.UserID
                      WHERE atd.TaskID = @TaskId
                      AND ta.CrewID = @CrewCommanderId
                      AND ta.IsActive = 1
                      AND u.UserID = @CrewCommanderId";

                    var results = await connection.QueryAsync<string>(
                        query,
                        new { TaskId = taskId, CrewCommanderId = authenticatedUserId }
                    );

                    if (!results.Any())
                    {
                        _logger.LogWarning("No ParcelLoadedAtBank found or unauthorized access for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                        throw new UnauthorizedAccessException("User is not authorized or no data found.");
                    }

                    var parcels = results
                     .Where(parcel => !string.IsNullOrWhiteSpace(parcel)) // Ignore empty/null data
                     .Select(parcel => new ParcelNo
                     {
                         ParcelNumber = parcel.Trim()
                     });

                    _logger.LogInformation("Successfully retrieved {Count} parcels for TaskID={TaskId}", parcels.Count(), taskId);
                    return parcels;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access while fetching ParcelLoadedAtBank for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching ParcelLoadedAtBank for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
        }
               
        public async Task<AtmParcelCountDTO> GetParclesCountsByTaskId(int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                const string query = @"
                SELECT 
                    SUM(LEN(ParcelLoadedAtBank) - LEN(REPLACE(ParcelLoadedAtBank, ',', '')) + 1) AS ParcelLoadedAtBank,
                    SUM(LEN(ParcelLoadedAtAtm) - LEN(REPLACE(ParcelLoadedAtAtm, ',', '')) + 1) AS ParcelLoadedAtAtm
                FROM AtmTaskDetail
                WHERE TaskID = @TaskID;";

                return await con.QueryFirstOrDefaultAsync<AtmParcelCountDTO>(query, new { TaskID = taskId })
                       ?? new AtmParcelCountDTO { ParcelLoadedAtBank= 0, ParcelLoadedAtAtm = 0 };
            }
        }

        public async Task<bool> ParcelUnLoadAtAtmStatusAsync(int crewCommanderId, int taskId, string status, AtmParcelLoadedAtATMDTO parcelDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Unload parcel at ATM: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
                using (var con = _db.CreateConnection())
                {

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'E');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);
                    parameters.Add("NextScreenId", parcelDTO.NextScreenId);
                    parameters.Add("Time", parcelDTO.Time);
                    parameters.Add("Lat", parcelDTO.Location?.Lat);
                    parameters.Add("Long", parcelDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("ParcelNumber", parcelDTO.ParcelNumber);


                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Parcel Unload Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }

        public async Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, AtmTaskFailedDTO failedDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Crew Task Failed Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}, FailureReason={FailureReason}",
                                        taskId, crewCommanderId, status, activityType, userId, failedDTO.FailureReason);
                using (var con = _db.CreateConnection())
                {
                    var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
                    if (currentScreenId == "-1")
                    {
                        _logger.LogWarning("TaskID={TaskId} is already marked as failed. Update prevented.", taskId);
                        return false; // Prevent update if task is already marked as failed
                    }

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'F');
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

                    _logger.LogDebug("Executing stored procedure: spAtmCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spAtmCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Crew Task Failed Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                 taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }
    }
} 
