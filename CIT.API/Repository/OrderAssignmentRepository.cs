using AutoMapper;
using CIT.API.Context;
using CIT.API.Models.OrderAssignment;
using CIT.API.Repository.IRepository;
using Dapper;

namespace CIT.API.Repository
{
    public class OrderAssignmentRepository : IOrderAssignmentRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        private readonly Random _random = new Random();

        public OrderAssignmentRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<int> AssignTeamsRandomly(DateTime assignDate, List<int> crewIds, List<int> leadVehicleIds, List<int> chaseVehicleIds)
        {
            if (!crewIds.Any() || !leadVehicleIds.Any() || !chaseVehicleIds.Any())
            {
                throw new InvalidOperationException("Crew IDs, Lead Vehicle IDs, and Chase Vehicle IDs cannot be empty");
            }
            using (var connection = _db.CreateConnection())
            {

                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var nextDay = assignDate.AddDays(1);

                    // Get orders for the next day
                    var ordersQuery = @"SELECT OrderRouteId, OrderType 
                                        FROM Orders 
                                        WHERE OrderDate = @NextDay";
                    var orders = await connection.QueryAsync<OrderRoutes>(ordersQuery, new { NextDay = nextDay }, transaction);

                    // Get ATM team combinations
                    var atmCombinationsQuery = @"SELECT CrewId, LeadVehicleId 
                                             FROM ATMTeamCombinations 
                                             WHERE @Date BETWEEN StartDate AND EndDate";
                    var atmCombinations = await connection.QueryAsync<ATMTeamCombination>(atmCombinationsQuery, new { Date = nextDay }, transaction);

                    var assignments = new List<TeamAssignment>();

                    foreach (var order in orders.OrderBy(x => Guid.NewGuid()))
                    {
                        int crewId, leadVehicleId, chaseVehicleId;

                        if (order.OrderType == "ATM")
                        {
                            var fixedCombination = GetRandomATMCombination(atmCombinations);
                            crewId = fixedCombination.CrewId;
                            leadVehicleId = fixedCombination.LeadVehicleId;
                            chaseVehicleId = GetRandomId(chaseVehicleIds);
                        }
                        else
                        {
                            crewId = GetRandomId(crewIds);
                            leadVehicleId = GetRandomId(leadVehicleIds);
                            chaseVehicleId = GetRandomId(chaseVehicleIds);
                        }

                        assignments.Add(new TeamAssignment
                        {
                            OrderRouteId = order.OrderRouteId,
                            CrewId = crewId,
                            LeadVehicleId = leadVehicleId,
                            ChaseVehicleId = chaseVehicleId,
                            AssignDate = assignDate
                        });
                    }

                    // Insert team assignments
                    var insertQuery = @"INSERT INTO TeamAssignments (OrderRouteId, CrewId, LeadVehicleId, ChaseVehicleId, AssignDate) 
                                    VALUES (@OrderRouteId, @CrewId, @LeadVehicleId, @ChaseVehicleId, @AssignDate)";
                    var rowsAffected = await connection.ExecuteAsync(insertQuery, assignments, transaction);

                    transaction.Commit();
                    return rowsAffected;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("An error occurred during team assignment.", ex);
                }

            }
        }

        private int GetRandomId(List<int> ids)
        {
            if (!ids.Any()) throw new InvalidOperationException("No IDs available for assignment");
            return ids[_random.Next(ids.Count)];
        }

        private ATMTeamCombination GetRandomATMCombination(IEnumerable<ATMTeamCombination> combinations)
        {
            var combinationList = combinations.ToList();
            if (!combinationList.Any())
                throw new InvalidOperationException("No valid ATM team combination found for the date");
            return combinationList[_random.Next(combinationList.Count)];
        }


    }
}
