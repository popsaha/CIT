namespace CIT.API.Models.Dto
{
    public class TaskGroupingRequestDTO
    {
        //declare class here
        public string? GroupName { get; set; }
        public int TaskID { get; set; }
        public DateTime? TaskDate { get; set; } = DateTime.Now;
    }
}
