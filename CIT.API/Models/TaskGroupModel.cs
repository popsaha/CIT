namespace CIT.API.Models
{
    public class TaskGroupModel
    {
        public int Id { get; set; }
        public string? GroupName { get; set; }
        public int TaskId { get; set; }
        public string? TaskDate { get; set; }
    }
}
