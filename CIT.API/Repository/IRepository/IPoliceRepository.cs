using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.Police;

namespace CIT.API.Repository.IRepository
{
    public interface IPoliceRepository
    {
        public Task<IEnumerable<PoliceMaster>> GetPolice();
        Task<int> AddPolice(PoliceCreateDTO policeDTO, int userId);
        Task<PoliceUpdateDTO> UpdatePolice(PoliceUpdateDTO police);
        Task<int> DeletePolice(int policeId, int deletedBy);
        Task<PoliceMaster> GetPoliceById(int policeId);
    }
}
