namespace CIT.API.Repository.IRepository
{
    public interface IJobRepository
    {
        Task GenerateRecurringOrdersAsync();
    }
}