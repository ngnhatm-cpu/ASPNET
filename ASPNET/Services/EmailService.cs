using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ASPNET.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var senderName = emailSettings["SenderName"] ?? "Manga Store";
        var senderEmail = emailSettings["SenderEmail"];
        var password = emailSettings["Password"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            throw new Exception("Cấu hình Email (SenderEmail hoặc Password) chưa được thiết lập trong appsettings.json");
        }
        
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(senderName, senderEmail));
        mimeMessage.To.Add(new MailboxAddress("", email));
        mimeMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message };
        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(emailSettings["SmtpServer"] ?? "smtp.gmail.com", int.Parse(emailSettings["Port"] ?? "587"), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, password);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log the error in a real application
            Console.WriteLine($"Error sending email: {ex.Message}");
            throw;
        }
    }
}
