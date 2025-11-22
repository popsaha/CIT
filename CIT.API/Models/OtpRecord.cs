namespace CIT.API.Models
{
    public class OtpRecord
    {
        
         public Guid OtpTransactionId { get; set; }   // DEFAULT NEWID()

         public string Mobile { get; set; }
         public Guid? TaskId { get; set; }

         public string Purpose { get; set; }          // ARRIVED | DELIVERY | etc.

         public string OtpHash { get; set; }
         public string Salt { get; set; }

         public DateTime CreatedAtUtc { get; set; }   // default: SYSUTCDATETIME()
         public DateTime ExpiresAtUtc { get; set; }
         public DateTime? UsedAtUtc { get; set; }

         public int Attempts { get; set; }            // default = 0
         public int MaxAttempts { get; set; }         // default = 5
         public string Status { get; set; }           // ACTIVE | USED | EXPIRED | LOCKED

         public string SenderId { get; set; }
         public string SmsProviderMessageId { get; set; }

         public Guid? CreatedByUserId { get; set; }
         public string CreatedByIp { get; set; }

    }
}
