using AutoMapper;
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
                    // Check for duplicate ParcelQR values
                    var parcelQRs = loadedDTO.Parcels.Select(p => p.ParcelQR).ToList();
                    if (parcelQRs.Count != parcelQRs.Distinct().Count())
                    {
                        _logger.LogWarning("Duplicate Parcel QR codes detected for TaskID={TaskId}", taskId);
                        Console.WriteLine("Duplicate Parcel QR codes detected.");
                        return false; // Duplicate ParcelQR codes detected
                    }

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
                    //parameters.Add("ParcelNumber", loadedDTO.ParcelNumber);
                    parameters.Add("ActivityType", activityType);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("PrcelLoadedAtBank", parcelsCsv);
                    //parameters.Add("ParcelsUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", loadedDTO.Parcels.Select(p => p.ParcelQR));

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
                    // Check for duplicate ParcelQR values
                    var parcelQRs = cassetteDTO.Parcels.Select(p => p.ParcelQR).ToList();
                    if (parcelQRs.Count != parcelQRs.Distinct().Count())
                    {
                        _logger.LogWarning("Duplicate Parcel QR codes detected for TaskID={TaskId}", taskId);
                        Console.WriteLine("Duplicate Parcel QR codes detected.");
                        return false; // Duplicate ParcelQR codes detected
                    }

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
                    //parameters.Add("ParcelNumber", cassetteDTO.ParcelNumber);

                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("PrcelLoadedAtBank", parcelsCsv);
                    parameters.Add("ParcelLoadedAtAtm", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", cassetteDTO.Parcels.Select(p => p.ParcelQR));
                    parameters.Add("ParcelLoadedAtAtm", parcelsCsv2);

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

                      // Fetch rows from the database
                    var results = await connection.QueryAsync<(string ParcelLoadedAtBank, string PickupReceiptNumber)>(
                        query,
                        new
                        {
                            TaskId = taskId,
                            CrewCommanderId = authenticatedUserId
                        }
                    );

                    if (!results.Any())
                    {
                        _logger.LogWarning("No ParcelLoadedAtBank found or unauthorized access for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                        throw new UnauthorizedAccessException("User is not authorized or no data found.");
                    }

                    // Process the parcel data
                    var parcels = results
                        .Where(row => !string.IsNullOrWhiteSpace(row.ParcelLoadedAtBank)) // Exclude NULL or empty rows
                        .SelectMany(row => row.ParcelLoadedAtBank.Split(',', StringSplitOptions.RemoveEmptyEntries) // Split by comma
                            .Select(parcelQr => new ParcelNo
                            {
                                ParcelQR = parcelQr.Trim()
                               
                            })); // Map to ParcelReceiptNo objects
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
                    // Check for duplicate ParcelQR values
                    var parcelQRs = parcelDTO.Parcels.Select(p => p.ParcelQR).ToList();
                    if (parcelQRs.Count != parcelQRs.Distinct().Count())
                    {
                        _logger.LogWarning("Duplicate Parcel QR codes detected for TaskID={TaskId}", taskId);
                        Console.WriteLine("Duplicate Parcel QR codes detected.");
                        return false; // Duplicate ParcelQR codes detected
                    }

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
                    //parameters.Add("ParcelNumber", parcelDTO.ParcelNumber);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelUnLoadedAtAtm", parcelsCsv);
                    //parameters.Add("ParcelsUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                    //parameters.Add("ParcelsUnloaded", parcelsCsv2);

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



        public async Task<bool> ParcelUnLoadAtBankStatusAsync(int crewCommanderId, int taskId, string status, ParcelUnLoadedAtBankDTO cassetteDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel loaded at ATM Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
                using (var con = _db.CreateConnection())
                {
                    // Check for duplicate ParcelQR values
                    var parcelQRs = cassetteDTO.Parcels.Select(p => p.ParcelQR).ToList();
                    if (parcelQRs.Count != parcelQRs.Distinct().Count())
                    {
                        _logger.LogWarning("Duplicate Parcel QR codes detected for TaskID={TaskId}", taskId);
                        Console.WriteLine("Duplicate Parcel QR codes detected.");
                        return false; // Duplicate ParcelQR codes detected
                    }

                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'G');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);
                    parameters.Add("NextScreenId", cassetteDTO.NextScreenId);
                    parameters.Add("Time", cassetteDTO.Time);
                    parameters.Add("Lat", cassetteDTO.Location?.Lat);
                    parameters.Add("Long", cassetteDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("DeliveryReceiptNumber", cassetteDTO.DeliveryReceiptNumber);
                    //parameters.Add("ParcelNumber", cassetteDTO.ParcelNumber);

                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelUnLoadedAtAtm", parcelsCsv);
                    parameters.Add("ParcelUnLoadedAtBank", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", cassetteDTO.Parcels.Select(p => p.ParcelQR));
                    parameters.Add("ParcelUnLoadedAtBank", parcelsCsv2);

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

        public async Task<IEnumerable<ParcelNo>> GetParcelUnLoadedAtAtmAsync(int taskId, int authenticatedUserId)
        {
            try
            {
                _logger.LogInformation("Fetching ParcelUnLoadAtAtm for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);

                using (var connection = _db.CreateConnection())
                {
                    string query = @"
                      SELECT atd.ParcelUnLoadAtAtm
                      FROM AtmTaskDetail atd
                      INNER JOIN Task t ON atd.TaskID = t.TaskID
                      INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
                      INNER JOIN UserMaster u ON ta.CrewID = u.UserID
                      WHERE atd.TaskID = @TaskId
                      AND ta.CrewID = @CrewCommanderId
                      AND ta.IsActive = 1
                      AND u.UserID = @CrewCommanderId";

                    // Fetch rows from the database
                    var results = await connection.QueryAsync<(string ParcelUnLoadedAtAtm, string PickupReceiptNumber)>(
                        query,
                        new
                        {
                            TaskId = taskId,
                            CrewCommanderId = authenticatedUserId
                        }
                    );

                    if (!results.Any())
                    {
                        _logger.LogWarning("No ParcelUnLoadedAtAtm found or unauthorized access for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                        throw new UnauthorizedAccessException("User is not authorized or no data found.");
                    }

                    // Process the parcel data
                    var parcels = results
                        .Where(row => !string.IsNullOrWhiteSpace(row.ParcelUnLoadedAtAtm)) // Exclude NULL or empty rows
                        .SelectMany(row => row.ParcelUnLoadedAtAtm.Split(',', StringSplitOptions.RemoveEmptyEntries) // Split by comma
                            .Select(parcelQr => new ParcelNo
                            {
                                ParcelQR = parcelQr.Trim()

                            })); // Map to ParcelReceiptNo objects
                    _logger.LogInformation("Successfully retrieved {Count} parcels for TaskID={TaskId}", parcels.Count(), taskId);
                    return parcels;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access while fetching ParcelUnLoadedAtAtm for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching ParcelUnLoadedAtAtm for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
        }

        //GetParcelDetail Method to fetch parcel data as comma-separated values
        public async Task<IEnumerable<ParcelReceiptNos>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb)
        {
            try
            {
                _logger.LogInformation("Fetching parcel details for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                // First, get the PickupType for the given TaskID
                const string pickupTypeQuery = @"SELECT PickupType FROM Task WHERE TaskID = @TaskId";

                using (var connection = _db.CreateConnection())
                {
                    int pickupType = await connection.ExecuteScalarAsync<int>(pickupTypeQuery, new { TaskId = taskId });

                    _logger.LogDebug("Retrieved PickupType={PickupType} for TaskID={TaskId}", pickupType, taskId);

                    // Determine which table to query based on PickupType
                    string query;
                    if (pickupType == 3)
                    {
                        query = @"
                        SELECT btd.ParcelLoadedAtBank, btd.PickupReceiptNumber
                        FROM AtmTaskDetail btd
                        INNER JOIN Task t ON btd.TaskID = t.TaskID
                        INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
                        INNER JOIN UserMaster u ON ta.CrewID = u.UserID
                        WHERE btd.TaskID = @TaskId
                        AND ta.CrewID = @CrewCommanderId
                        AND ta.IsActive = 1
                        AND u.UserID = @CrewCommanderId";
                    }
                    else
                    {
                        _logger.LogWarning("Invalid PickupType={PickupType} for TaskID={TaskId}", pickupType, taskId);
                        throw new Exception("Invalid PickupType.");
                    }

                    // Fetch rows from the database
                    var results = await connection.QueryAsync<(string ParcelLoaded, string PickupReceiptNumber)>(
                            query,
                            new
                            {
                                TaskId = taskId,
                                CrewCommanderId = authenticatedUserId
                            }
                        );

                    // If no rows are returned, the user isn't authorized or the task doesn't exist
                    if (!results.Any())
                    {
                        _logger.LogWarning("Unauthorized access attempt or invalid TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                        throw new UnauthorizedAccessException("User is not authorized to access this task or task ID is invalid.");
                    }

                    // Process the parcel data
                    var parcels = results
                      .Where(row => !string.IsNullOrWhiteSpace(row.ParcelLoaded))
                      .SelectMany(row => row.ParcelLoaded.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(parcelQr => new ParcelReceiptNos
                          {
                              ParcelQR = parcelQr.Trim(),
                              PickupReceiptNumber = row.PickupReceiptNumber
                          }));
                      return parcels;

                    //string pickupReceiptNumber = results.First().PickupReceiptNumber;

                    //return new ParcelReceiptNos
                    //{
                    //    PickupReceiptNumber = pickupReceiptNumber,
                    //    ParcelQRs = allParcels
                    //};

                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access while fetching parcels for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching parcels for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
        }

        //GetParcelDetail Method to fetch parcel data as comma-separated values
        public async Task<IEnumerable<ParcelNo>> GetParcelUnloadedAsync(int taskId, int authenticatedUserId, int userIdFromDb)
        {
            try
            {
                _logger.LogInformation("Fetching parcel details for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                // First, get the PickupType for the given TaskID
                const string pickupTypeQuery = @"SELECT PickupType FROM Task WHERE TaskID = @TaskId";

                using (var connection = _db.CreateConnection())
                {
                    int pickupType = await connection.ExecuteScalarAsync<int>(pickupTypeQuery, new { TaskId = taskId });

                    _logger.LogDebug("Retrieved PickupType={PickupType} for TaskID={TaskId}", pickupType, taskId);

                    // Determine which table to query based on PickupType
                    string query;
                    if (pickupType == 3)
                    {
                        query = @"
                        SELECT btd.ParcelUnloadAtAtm
                        FROM AtmTaskDetail btd
                        INNER JOIN Task t ON btd.TaskID = t.TaskID
                        INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
                        INNER JOIN UserMaster u ON ta.CrewID = u.UserID
                        WHERE btd.TaskID = @TaskId
                        AND ta.CrewID = @CrewCommanderId
                        AND ta.IsActive = 1
                        AND u.UserID = @CrewCommanderId";
                    }
                    else
                    {
                        _logger.LogWarning("Invalid PickupType={PickupType} for TaskID={TaskId}", pickupType, taskId);
                        throw new Exception("Invalid PickupType.");
                    }

                    // Fetch rows from the database
                    var results = await connection.QueryAsync<(string ParcelLoaded, string PickupReceiptNumber)>(
                            query,
                            new
                            {
                                TaskId = taskId,
                                CrewCommanderId = authenticatedUserId
                            }
                        );

                    // If no rows are returned, the user isn't authorized or the task doesn't exist
                    if (!results.Any())
                    {
                        _logger.LogWarning("Unauthorized access attempt or invalid TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                        throw new UnauthorizedAccessException("User is not authorized to access this task or task ID is invalid.");
                    }

                    // Process the parcel data
                    var parcels = results
                        .Where(row => !string.IsNullOrWhiteSpace(row.ParcelLoaded)) // Exclude NULL or empty rows
                        .SelectMany(row => row.ParcelLoaded.Split(',', StringSplitOptions.RemoveEmptyEntries) // Split by comma
                            .Select(parcelQr => new ParcelNo
                            {
                                ParcelQR = parcelQr.Trim(),
                            })); // Map to ParcelReceiptNo objects
                    _logger.LogInformation("Successfully retrieved {Count} parcels for TaskID={TaskId}", parcels.Count(), taskId);
                    return parcels;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access while fetching parcels for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching parcels for TaskID={TaskId} by UserID={UserId}", taskId, authenticatedUserId);
                throw;
            }
        }
    }
} 
