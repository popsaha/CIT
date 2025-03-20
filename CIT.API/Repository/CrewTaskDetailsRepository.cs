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
using CIT.API.Models.Dto.Order;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace CIT.API.Repository
{
    public class CrewTaskDetailsRepository : ICrewTaskDetailsRepository
    {
        private readonly DapperContext _db;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CrewTaskDetailsRepository> _logger;

        public CrewTaskDetailsRepository(DapperContext db, IMapper mapper, IHttpContextAccessor httpContextAccessor, ILogger<CrewTaskDetailsRepository> logger)
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


        // New method to get tasks for a specific Crew Commander
        public async Task<IEnumerable<CrewTaskDetailsDTO>> GetCrewTasksByCommanderIdAsync(int crewCommanderId, int userId, DateTime? orderDate = null)
        {
            try
            {
                _logger.LogInformation("Fetching crew tasks for CrewCommanderId: {CrewCommanderId}, UserId: {UserId}, OrderDate: {OrderDate}", crewCommanderId, userId, orderDate);

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
                    _logger.LogInformation("Retrieved {Count} tasks for CrewCommanderId: {CrewCommanderId}", crewTasks.Count(), crewCommanderId);
                    return crewTasks.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching crew tasks for CrewCommanderId: {CrewCommanderId}, UserId: {UserId}", crewCommanderId, userId);
                throw;
            }
        }

        public async Task<CrewTaskDetailsByTaskIdDTO> GetTaskDetailsByTaskIdAsync(int crewCommanderId, int taskId, int userId)
        {
            try
            {
                _logger.LogInformation("Fetching task details for TaskId: {TaskId}, CrewCommanderId: {CrewCommanderId}, UserId: {UserId}", taskId, crewCommanderId, userId);

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
                    _logger.LogInformation("Successfully retrieved task details for TaskId: {TaskId}", taskId);
                    return taskDetails; // Returns null if task isn't found or crew commander isn't authorized
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching task details for TaskId: {TaskId}, CrewCommanderId: {CrewCommanderId}, UserId: {UserId}", taskId, crewCommanderId, userId);
                throw;
            }
        }


        public async Task<string> GetNextScreenIdByTaskId(int taskId)
        {
            try
            {
                _logger.LogInformation("Fetching next ScreenId for TaskId: {TaskId}", taskId);
                // Get the current ScreenId
                var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);


                // Extract the prefix (everything before the last hyphen)
                var lastHyphenIndex = currentScreenId.LastIndexOf('-');
                _logger.LogInformation("Current ScreenId for TaskId {TaskId}: {CurrentScreenId}", taskId, currentScreenId);

                if (lastHyphenIndex > 0)  // Ensure there's a valid prefix
                {
                    string prefix = currentScreenId.Substring(0, lastHyphenIndex); // Extract prefix
                    string numberPart = currentScreenId.Substring(lastHyphenIndex + 1); // Extract number

                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        int nextNumber = currentNumber + 1;
                        return $"{prefix}-{nextNumber}";
                    }
                }
                // Default value if ScreenId is in an unexpected format
                return $"{currentScreenId}-2";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching next ScreenId for TaskId: {TaskId}", taskId);
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

        public async Task<bool> UpdateTaskStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO updateDTO, string activityType, int userId)
        {

            //var currentScreenId = await GetCurrentScreenIdByTaskId(taskId);
            //if (currentScreenId == "1")
            //{
            //    return false; // Prevent update if task is already marked as failed
            //}
            try
            {
                _logger.LogInformation("Updating Task Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);


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

                    _logger.LogDebug("Executing stored procedure: spCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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


        public async Task<bool> parcelLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskParcelDTO parcelDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel Load Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}, PickupReceiptNumber={PickupReceiptNumber}",
                                        taskId, crewCommanderId, status, activityType, userId, parcelDTO.PickupReceiptNumber);

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
                    parameters.Add("NextScreenId", parcelDTO.NextScreenId);
                    parameters.Add("Time", parcelDTO.Time);
                    parameters.Add("Lat", parcelDTO.Location?.Lat);
                    parameters.Add("Long", parcelDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("PickupReceiptNumber", parcelDTO.PickupReceiptNumber);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelsLoaded", parcelsCsv);
                    //parameters.Add("ParcelsUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                    //parameters.Add("ParcelsUnloaded", parcelsCsv2);

                    _logger.LogDebug("Executing stored procedure: spCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<bool> parcelUnLoadStatusAsync(int crewCommanderId, int taskId, string status, CrewTaskUnloadedParcelDTOs parcelDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Parcel Unload Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}, DeliveryReceiptNumber={DeliveryReceiptNumber}",
                                        taskId, crewCommanderId, status, activityType, userId, parcelDTO.DeliveryReceiptNumber);
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
                    parameters.Add("NextScreenId", parcelDTO.NextScreenId);
                    parameters.Add("Time", parcelDTO.Time);
                    parameters.Add("Lat", parcelDTO.Location?.Lat);
                    parameters.Add("Long", parcelDTO.Location?.Long);
                    parameters.Add("ActivityType", activityType);
                    parameters.Add("DeliveryReceiptNumber", parcelDTO.DeliveryReceiptNumber);

                    // Create a comma-separated string from unique ParcelQR values
                    var parcelsCsv = string.Join(",", parcelQRs);
                    parameters.Add("ParcelsLoaded", parcelsCsv);
                    parameters.Add("ParcelsUnloaded", parcelsCsv);

                    var parcelsCsv2 = string.Join(",", parcelDTO.Parcels.Select(p => p.ParcelQR));
                    parameters.Add("ParcelsUnloaded", parcelsCsv2);

                    _logger.LogDebug("Executing stored procedure: spCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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


        public async Task<bool> crewTaskFailedAsync(int crewCommanderId, int taskId, string status, CrewTaskFailedStatusDTO failedDTO, string activityType, int userId)
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

                    _logger.LogDebug("Executing stored procedure: spCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);

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

        public async Task<bool> arrivedDeliveryAsync(int crewCommanderId, int taskId, string status, CrewTaskStatusUpdateDTO arrivedDTO, string activityType, int userId)
        {
            try
            {
                _logger.LogInformation("Updating Arrived Delivery Status: TaskID={TaskId}, CrewCommanderID={CrewCommanderId}, Status={Status}, ActivityType={ActivityType}, UserID={UserId}",
                                        taskId, crewCommanderId, status, activityType, userId);
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

                    _logger.LogDebug("Executing stored procedure: spCrewTaskDetails with parameters: {Parameters}", parameters);

                    var result = await con.ExecuteAsync("spCrewTaskDetails", parameters, commandType: CommandType.StoredProcedure);
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
                    if (pickupType == 1)
                    {
                        query = @"
                        SELECT ctd.ParcelsLoaded, ctd.PickupReceiptNumber
                        FROM CitTaskDetail ctd
                        INNER JOIN Task t ON ctd.TaskID = t.TaskID
                        INNER JOIN TeamAssignments ta ON t.OrderID = ta.OrderID
                        INNER JOIN UserMaster u ON ta.CrewID = u.UserID
                        WHERE ctd.TaskID = @TaskId
                        AND ta.CrewID = @CrewCommanderId
                        AND ta.IsActive = 1
                        AND u.UserID = @CrewCommanderId";
                    }
                    else if (pickupType == 2)
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
                    var results = await connection.QueryAsync<(string ParcelsLoaded, string PickupReceiptNumber)>(
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
                        .Where(row => !string.IsNullOrWhiteSpace(row.ParcelsLoaded)) // Exclude NULL or empty rows
                        .SelectMany(row => row.ParcelsLoaded.Split(',', StringSplitOptions.RemoveEmptyEntries) // Split by comma
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
    
        //GetParclesCountsByTaskId

        public async Task<ParcelCountDTO> GetParclesCountsByTaskId(int taskId)
        {
            using (var con = _db.CreateConnection())
            {
                const string query = @"
        SELECT 
            SUM(LEN(ParcelsLoaded) - LEN(REPLACE(ParcelsLoaded, ',', '')) + 1) AS ParcelsLoaded,
            SUM(LEN(ParcelsUnloaded) - LEN(REPLACE(ParcelsUnloaded, ',', '')) + 1) AS ParcelsUnloaded
        FROM CitTaskDetail
        WHERE TaskID = @TaskID;";

                return await con.QueryFirstOrDefaultAsync<ParcelCountDTO>(query, new { TaskID = taskId })
                       ?? new ParcelCountDTO { ParcelsLoaded = 0, ParcelsUnloaded = 0 };
            }
        }
     

    }
}