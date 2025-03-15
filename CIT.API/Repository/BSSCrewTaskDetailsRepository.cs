using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.Dto.BSSCrewTaskDetails;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;
using static Hangfire.Storage.JobStorageFeatures;

namespace CIT.API.Repository
{
    public class BSSCrewTaskDetailsRepository : IBSSCrewTaskDetailsRepository
    {
        private readonly DapperContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BSSCrewTaskDetailsRepository> _logger;

        public BSSCrewTaskDetailsRepository(DapperContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor, ILogger<BSSCrewTaskDetailsRepository> logger)
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

        // New method to call the static method
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
                    "BSS-Start" => "BSS-Arrived",
                    "BSS-Arrived" => "BSS-SaveAmount",
                    "BSS-SaveAmount" => "BSS-Loaded",
                    "BSS-Loaded" => "BSS-ArrivedDelivery",
                    "BSS-ArrivedDelivery" => "BSS-Unloaded",
                    "BSS-Unloaded" => "BSS-Completed",
                    "BSS-Completed" => "1",
                    /*_ => "-1"*/ // Default fallback
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching next ScreenId for TaskId: {TaskId}", taskId);
                throw;
            }
        }
        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, BSSCrewTaskStatusUpdateDTO updateDTO, string activityType, int userId)
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

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<bool> SaveAmountAsync(int crewCommanderId, int taskId, string status, BssSaveAmountDTO bssCountStatusDTO, string activityType, int userId, double TotalAmount)
        {
            try
            {
                _logger.LogInformation("Updating Task Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);

                using (var con = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'B');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);
                    parameters.Add("NextScreenId", bssCountStatusDTO.NextScreenId); // Set ScreenId based on activityType  // Set ScreenId to 1 as required by the update
                    parameters.Add("LocalAmount", bssCountStatusDTO.LocalAmount);
                    parameters.Add("TotalAmount", TotalAmount);                          
                    parameters.Add("Time", bssCountStatusDTO.Time);  // Pass the start time from DTO
                    parameters.Add("Lat", bssCountStatusDTO.Location?.Lat);  // Pass Latitude if available
                    parameters.Add("Long", bssCountStatusDTO.Location?.Long);  // Pass Longitude if available
                    parameters.Add("ActivityType", activityType);

                    //Local currency
                    parameters.Add("Thousand", bssCountStatusDTO.Denominations.Thousand);
                    parameters.Add("FiveHundred", bssCountStatusDTO.Denominations.FiveHundred);
                    parameters.Add("TwoHundred", bssCountStatusDTO.Denominations.TwoHundred);
                    parameters.Add("OneHundred", bssCountStatusDTO.Denominations.OneHundred);
                    parameters.Add("Fifty", bssCountStatusDTO.Denominations.Fifty);
                    parameters.Add("Forty", bssCountStatusDTO.Denominations.Forty);
                    parameters.Add("Twenty", bssCountStatusDTO.Denominations.Twenty);
                    parameters.Add("Ten", bssCountStatusDTO.Denominations.Ten);
                    parameters.Add("Five", bssCountStatusDTO.Denominations.Five);
                    parameters.Add("One", bssCountStatusDTO.Denominations.One);

                    //other Foreign currency
                    parameters.Add("USD", bssCountStatusDTO.Currency.USD);
                    parameters.Add("GBP", bssCountStatusDTO.Currency.GBP);
                    parameters.Add("EURO", bssCountStatusDTO.Currency.EURO);
                    parameters.Add("ZAR", bssCountStatusDTO.Currency.ZAR);
                    parameters.Add("Others", bssCountStatusDTO.Currency.Others);

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, BssParcelLoadDTO parcelDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel Load Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}, PickupReceiptNumber={PickupReceiptNumber}",
                                                       taskId, crewCommanderId, status, activityType, userId, parcelDTO.PickupReceiptNumber);
                
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
                    parameters.Add("Flag", 'C');
                    parameters.Add("CrewCommanderId", crewCommanderId);
                    parameters.Add("TaskId", taskId);
                    parameters.Add("Status", status);
                    parameters.Add("UserId", userId);
                    parameters.Add("NextScreenId", parcelDTO.NextScreenId);
                    parameters.Add("Time", parcelDTO.Time);
                    parameters.Add("Lat", parcelDTO.Location?.Lat);
                    parameters.Add("Long", parcelDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("PickupReceiptNumber", parcelDTO.PickupReceiptNumber);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelLoaded", parcelsCsv);
                    //parameters.Add("ParcelsUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                    //parameters.Add("ParcelsUnloaded", parcelsCsv2);

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

                    return result > 0;
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occurred while updating Parcel Load Status for TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, ActivityType={ActivityType}, UserID={UserId}",
                                     taskId, crewCommanderId, activityType, userId);
                throw;
            }
        }

        public async Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, BSSCrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Arrived Delivery Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
                using (var con = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", 'D');
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

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
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

        // Method to fetch parcel data as comma-separated values
        public async Task<IEnumerable<ParcelReceiptNo>> GetParcelAsync(int taskId, int authenticatedUserId, int userIdFromDb)
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
                     if (pickupType == 2)
                    {
                        query = @"
                        SELECT btd.ParcelLoaded, btd.PickupReceiptNumber
                        FROM BssTaskDetail btd
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
                            .Select(parcelQr => new ParcelReceiptNo
                            {
                                ParcelQR = parcelQr.Trim(),
                                PickupReceiptNumber = row.PickupReceiptNumber
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
        public async Task<bool> parcelUnLoadStatusAsync(int crewCommanderId, int taskId, string status, BssParcelUnloadDTO parcelDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel Unload Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}, DeliveryReceiptNumber={DeliveryReceiptNumber}",
                                        taskId, crewCommanderId, status, activityType, userId, parcelDTO.DeliveryReceiptNumber);
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
                    parameters.Add("DeliveryReceiptNumber", parcelDTO.DeliveryReceiptNumber);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelLoaded", parcelsCsv);
                    parameters.Add("ParcelUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                    parameters.Add("ParcelUnloaded", parcelsCsv2);

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<BssParcelCountDTO> GetParclesCountsByTaskId(int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                const string query = @"
                SELECT 
                    SUM(LEN(ParcelLoaded) - LEN(REPLACE(ParcelLoaded, ',', '')) + 1) AS ParcelLoaded,
                    SUM(LEN(ParcelUnloaded) - LEN(REPLACE(ParcelUnloaded, ',', '')) + 1) AS ParcelUnloaded
                FROM BssTaskDetail
                WHERE TaskID = @TaskID;";

                return await con.QueryFirstOrDefaultAsync<BssParcelCountDTO>(query, new { TaskID = taskId })
                       ?? new BssParcelCountDTO { ParcelLoaded = 0, ParcelUnloaded = 0 };
            }
        }

        public async Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, BssTaskFailedDTO failedDTO, string activityType, int userId)
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

                    _logger.LogDebug("Executing stored procedure: spBSSCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spBSSCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<int> GetTotalAmountByTaskId(int taskId)
        {
            const string getAmountQuery = @"SELECT ISNULL(SUM(TotalAmount), 0) FROM BssTaskDetail WHERE TaskID = @TaskId";

            using (var con = _db.CreateConnection())
            {
                return await con.ExecuteScalarAsync<int>(getAmountQuery, new { TaskId = taskId });
            }
        }
    
    }
}
