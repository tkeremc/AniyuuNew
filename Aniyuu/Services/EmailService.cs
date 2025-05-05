using Aniyuu.Interfaces;
using Aniyuu.ViewModels;
using NLog;

namespace Aniyuu.Services;

public class EmailService(IMessagePublisherService messagePublisherService) : IEmailService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public async Task SendWelcomeEmail(string email, string username, int code, CancellationToken cancellationToken)
    {
        try
        {
            var message = new EmailMessageViewModel()
            {
                To = email,
                Subject = "Aniyuu'ya Hoş Geldin!",
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
                Subject = "Aniyuu Hesabınızı doğrulayın!",
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
                Subject = "Şifre değiştirme talebi",
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
}