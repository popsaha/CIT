namespace CIT.API.Models
{
    public class TaskMaster
    {
        public string OrderId { get; set; }
        public int OrderTypeID { get; set; }
        public int PriorityId { get; set; }
        public int PickUpTypeId { get; set; }
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
        public List<TaskBranch> taskbranchlist { get; set; }
        public List<VaultLovationMaster> vaultLovationMasters { get; set; }
    }
    public class TaskBranch
    {
        public int BranchID { get; set; }
        public string BranchName { get; set; }
    }
    public class VaultLovationMaster
    {
        public int VaultID { get; set; }
        public string VaultName { get; set; }
    }
}
