namespace CIT.API.Models
{
    public class TaskGroupRequestModel
    {
        public string? GroupName { get; set; }
        public int TaskID { get; set; }
        public DateTime? TaskDate { get; set; } = DateTime.Now;
    }
}
