using Aniyuu.Exceptions;
using Aniyuu.Interfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.Utils;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NLog;

namespace Aniyuu.Services;

public class EmailService(IActivationService activationService) : IEmailService
{
    private readonly string _smtpHost = AppSettingConfig.Configuration["MailSettings:Host"]!;
    private readonly int _smtpPort = Convert.ToInt32(AppSettingConfig.Configuration["MailSettings:Port"]!);
    private readonly string _smtpUser = AppSettingConfig.Configuration["MailSettings:User"]!;
    private readonly string _smtpPass = AppSettingConfig.Configuration["MailSettings:Password"]!;
    private readonly string _smtpFrom = AppSettingConfig.Configuration["MailSettings:NoReplySender"]!;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task SendEmail(string to, string subject, string templateName, Dictionary<string, string> placeholders, bool isHtml = true)
    {
        try
        {
            var templatePath = Path.Combine("Templates", $"{templateName}.html");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template bulunamadı: {templatePath}");

            var body = await File.ReadAllTextAsync(templatePath);

            foreach (var kvp in placeholders)
            {
                body = body.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Aniyuu.com", _smtpFrom));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            var builder = new BodyBuilder
            {
                HtmlBody = body,
            };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_smtpUser, _smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            Logger.Error("[EmailService.SendEmail] Error during sending email");
            throw new AppException("Server error occurred on sending email", 500);
        }
    }

    public async Task SendWelcomeEmail(string email, string username, CancellationToken cancellationToken)
    {
        try
        {
            await SendEmail(
                to: email,
                subject: "Aniyuu'ya Hoş Geldin!",
                templateName: "WelcomeEmail",
                placeholders: new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "code", Convert.ToString(activationService.GenerateActivationCode(email, cancellationToken)) }
                });
        }
        catch (Exception e)
        {
            Logger.Error("[EmailService.SendWelcomeEmail] Error during sending email");
        }
    }

    public async Task ResendConfirmationEmail(string email, string username, CancellationToken cancellationToken)
    {
        try
        {
            await SendEmail(
                to: email,
                subject: "Aniyuu Hesabınızı doğrulayın!",
                templateName: "ActivationCodeEmail",
                placeholders: new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "code", Convert.ToString(activationService.GenerateActivationCode(email, cancellationToken)) }
                });
        }
        catch (Exception e)
        {
            Logger.Error("[EmailService.ResendConfirmationEmail] Error during sending email");
        }
    }

    public async Task PasswordResetEmail(string email, string username, CancellationToken cancellationToken)
    {
        try
        {
            await SendEmail(
                to: email,
                subject: "Şifre değiştirme talebiniz",
                templateName: "WelcomeEmail",
                placeholders: new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                });
        }
        catch (Exception e)
        {
            Logger.Error("[EmailService.PasswordResetEmail] Error during sending email");
        }
    }
}