using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CIT.API.Repository
{
    public class Report : IReport
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public Report(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<IEnumerable<ReportDetails>> GetAllReportData()
        {
            IEnumerable<ReportDetails> _Report;
          
            using (var connection =  _db.CreateConnection())
            {
                string storedProcedureName = "Proc_GetAllReportData";
                
                _Report = await connection.QueryAsync<ReportDetails>(storedProcedureName, commandType: CommandType.StoredProcedure);
               
            }
            return _Report;
        }

        public async Task<IEnumerable<ReportDetails>> GetFilteredReportData(ReportDetailsParam _reportDetails)
        {
            IEnumerable<ReportDetails> _Report;

            using (var connection = _db.CreateConnection())
            {
                string storedProcedureName = "Proc_GetReportData";

                var parametrs = new DynamicParameters();
                parametrs.Add("Branch", _reportDetails.Branchid == "" ? null : _reportDetails.Branchid, DbType.String, ParameterDirection.Input);
                parametrs.Add("CustID", _reportDetails.Customerid == "" ? null : _reportDetails.Customerid, DbType.String, ParameterDirection.Input);
                parametrs.Add("Service", _reportDetails.PickupTypeid == "" ? null : _reportDetails.PickupTypeid, DbType.String, ParameterDirection.Input);
                parametrs.Add("fromdate", _reportDetails.fromDate == "" ? null : _reportDetails.fromDate, DbType.Date, ParameterDirection.Input);
                parametrs.Add("todate", _reportDetails.ToDate == "" ? null : _reportDetails.ToDate, DbType.Date, ParameterDirection.Input);

                _Report = await connection.QueryAsync<ReportDetails>(storedProcedureName, parametrs, commandType: CommandType.StoredProcedure);

            }
            return _Report;
        }
    }
}
