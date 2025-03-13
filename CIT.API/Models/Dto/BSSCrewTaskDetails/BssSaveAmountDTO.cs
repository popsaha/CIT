using CIT.API.Models.Dto.CrewTaskDetails;

namespace CIT.API.Models.Dto.BSSCrewTaskDetails
{
    public class BssSaveAmountDTO
    {
        public string NextScreenId { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
        //local currency 
        public int SaveAmount { get; set; }
        //Local currency and other currency
        public int TotalAmount { get; set; }
        public Location Location { get; set; }
        public DenominationBreakdown Denominations { get; set; }
        public Currency Currency { get; set; }

    }
    public class DenominationBreakdown
    {
        public int? Thousand { get; set; } // 1000/=
        public int? FiveHundred { get; set; } // 500/=
        public int? TwoHundred { get; set; } // 200/=
        public int? OneHundred { get; set; } // 100/=
        public int? Fifty { get; set; } // 50/=
        public int? Forty { get; set; } // 40/=
        public int? Twenty { get; set; } // 20/=
        public int? Ten { get; set; } // 10/=
        public int? Five { get; set; } // 5/=
        public int? One { get; set; } // 1/=
    }
    public class Currency
    {
        public int? USD { get; set; }
        public int? GBP { get; set; }
        public int? EURO { get; set; }
        public int? ZAR { get; set; }
        public int? Others { get; set; }
    }
}