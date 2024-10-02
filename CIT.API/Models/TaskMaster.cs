namespace CIT.API.Models
{
    public class TaskMaster
    {
        public string OrderId { get; set; }
        public int OrderTypeID { get; set; }
        public int PriorityId { get; set; }
        public int PickUpTypeId { get; set; }
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public int BranchID { get; set; }
        public int CustomerRecipiantId { get; set; }
        public int CustomerRecipiantLocationId { get; set; }
        public int RepeatId { get; set; }
        public string OrderCreateDate { get; set; }
        public string RepeatDaysName { get; set; }
        public string EndOnDate { get; set; }
        public int VaultID { get; set; }
        public bool isVault { get; set; }
        public bool isVaultFinal { get; set; }
        public int OrderRouteId { get; set; }
        public bool NewVehicleRequired { get; set; }
        public int OrderSubRouteID { get; set; }
        public int NoOfVehicles { get; set; }
        public int SubRouteNumber { get; set; }
        public bool fullDayCheck { get; set; }
        public List<TaskBranch> taskbranchlist { get; set; }
        public List<VaultLocationMaster> vaultLocationMasters { get; set; }
        public List<OrderRoutes> Orderrouteslst { get; set; }
    }
    public class TaskBranch
    {
        public int BranchID { get; set; }
        public string BranchName { get; set; }
    }
    public class VaultLocationMaster
    {
        public int VaultID { get; set; }
        public string VaultName { get; set; }
    }
    public class OrderRoutes
    {
        public int OrderRouteId { get; set; }
        public string RouteName { get; set; }
    }
}
