namespace CIT.API.Models.Dto.AtmCrewTaskDetails
{
    public class AtmCrewTaskOffline
    {
        public AtmCollection AtmCollection { get; set; }

    }
    public class AtmLocation
    {
        public string Lat { get; set; }
        public string Long { get; set; }
    }
    public class BaseStage
    {
        public string Status { get; set; }
        public DateTime Time { get; set; }
        public AtmLocation Location { get; set; }
    }
    public class StageWithReceipt : BaseStage
    {
        public string PickupReceiptNumber { get; set; }
        public List<string> Parcels { get; set; }
    }
    public class StageWithDeliveryReceipt : BaseStage
    {
        public string DeliveryReceiptNumber { get; set; }
        public List<string> Parcels { get; set; }
    }
    public class StageWithParcels : BaseStage
    {
        public List<string> Parcels { get; set; }
    }
    public class AtmCollection
    {
        public BaseStage Start { get; set; }
        public BaseStage Arrived { get; set; }
        public StageWithReceipt LoadedAtBank { get; set; }
        public BaseStage ArrivedDelivery { get; set; }
        public StageWithParcels LoadedAtAtm { get; set; }
        public StageWithParcels UnloadedAtAtm { get; set; }
        public StageWithDeliveryReceipt Complete { get; set; }
        public BaseStage Fail { get; set; } // Optional: will be null if no failure
    }

}
