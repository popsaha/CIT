using CIT.API.Models;

namespace CIT.API.Repository.IRepository
{
    public interface IReport
    {
         Task<IEnumerable<ReportDetails>> GetAllReportData();
        Task<IEnumerable<ReportDetails>> GetFilteredReportData(ReportDetailsParam _reportDetails);
    }
}
