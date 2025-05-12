using Aniyuu.Interfaces;
using Aniyuu.Interfaces.UserInterfaces;
using Aniyuu.ViewModels;
using NLog;

namespace Aniyuu.Services;

public class EmailService(IMessagePublisherService messagePublisherService,
    ICurrentUserService currentUserService) : IEmailService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task SendWelcomeEmail(string email, string username, int code, CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessageViewModel()
            {
                To = email,
                Subject = "Aniyuu.com | Aniyuu'ya Hoş Geldin!",
                TemplateName = "WelcomeEmail",
                UsedPlaceholders = new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "code", Convert.ToString(code) }
                }
            };
            
            await messagePublisherService.PublishAsync(message, "email-exchange", "notification");
        }
        catch (Exception e)
        {
            Logger.Error($"[EmailService.SendWelcomeEmail] Error during sending email. Email:{email}");
        }
    }

    public async Task ResendConfirmationEmail(string email, string username, int code, CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessageViewModel()
            {
                To = email,
                Subject = "Aniyuu.com | Aniyuu Hesabınızı doğrulayın!",
                TemplateName = "ActivationCodeEmail",
                UsedPlaceholders = new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "code", Convert.ToString(code) }
                }
            };
            
            await messagePublisherService.PublishAsync(message, "email-exchange", "notification");
        }

        catch (Exception e)
        {
            Logger.Error($"[EmailService.ResendConfirmationEmail] Error during sending email. Email:{email}");
        }
    }

    public async Task PasswordResetEmail(string email, string username, string token, CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessageViewModel()
            {
                To = email,
                Subject = "Aniyuu.com | Şifre değiştirme talebi",
                TemplateName = "PasswordResetEmail",
                UsedPlaceholders = new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "token", token }
                }
            };
            
            await messagePublisherService.PublishAsync(message, "email-exchange", "notification");
        }
        catch (Exception e)
        {
            Logger.Error($"[EmailService.PasswordResetEmail] Error during sending email. Email:{email}");
        }
    }

    public async Task NewDeviceLoginEmail(string email, string username,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessageViewModel()
            {
                To = email,
                Subject = "Aniyuu.com | Yeni cihaz girişi yapıldı!",
                TemplateName = "NewLoginEmail",
                UsedPlaceholders = new Dictionary<string, string>
                {
                    { "username", username },
                    { "email", email },
                    { "date", DateTime.UtcNow.ToString("dd/MM/yyyy") },
                    { "time", DateTime.UtcNow.ToString("hh:mm:ss") },
                    { "ip_address", currentUserService.GetIpAddress() }, //sus, maybe im gonna delete this part
                    { "location", currentUserService.GetUserAddress() },
                    { "browser", currentUserService.GetBrowserData() },
                    { "operating_system", currentUserService.GetOSData() }
                }
            };

            await messagePublisherService.PublishAsync(message, "email-exchange", "notification");
        }
        catch (Exception e)
        {
            Logger.Error($"[EmailService.NewDeviceLogin] Error during sending email. Email:{email}");
        }
    }
}