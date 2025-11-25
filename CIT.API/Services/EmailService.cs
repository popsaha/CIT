using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace CIT.API.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var host = _config["EmailSettings:Host"];
        var port = int.Parse(_config["EmailSettings:Port"]);
        var enableSsl = bool.Parse(_config["EmailSettings:EnableSSL"]);
        var userName = _config["EmailSettings:UserName"];
        var password = _config["EmailSettings:Password"];

        using (var smtpClient = new SmtpClient(host))
        {
            smtpClient.Port = port;
            smtpClient.EnableSsl = enableSsl;
            smtpClient.Credentials = new NetworkCredential(userName, password);

            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(userName, "RMS");
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (SmtpException ex)
                {
                    throw new Exception($"Email sending failed: {ex.Message}", ex);
                }
            }
        }
    }
}
