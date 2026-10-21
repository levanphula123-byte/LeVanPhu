using System.Net;
using System.Net.Mail;
namespace LeVanPhu.Api.Services;

public class EmailService(IConfiguration config)
{
    public async Task SendOtpAsync(string to, string code)
    {
        var smtp = config.GetSection("Smtp");
        var user = smtp["User"];
        var password = smtp["Password"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP chưa cấu hình. Thiết lập Smtp:User, Smtp:Password và Smtp:From bằng user-secrets.");

        using var message = new MailMessage(from, to,
            "Mã OTP đăng ký/đăng nhập",
            $"Mã OTP của bạn là {code}. Mã chỉ có hiệu lực trong thời gian ngắn; hãy nhập ngay trên ứng dụng. Không chia sẻ mã này.");
        using var client = new SmtpClient(smtp["Host"] ?? "smtp.gmail.com",
            int.TryParse(smtp["Port"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) && ssl,
            Credentials = new NetworkCredential(user, password),
            Timeout = 15000
        };
        await client.SendMailAsync(message);
    }
}
