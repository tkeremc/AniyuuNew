namespace Aniyuu.Interfaces;

public interface IEmailService
{
    Task SendEmail(string to, string subject, string templateName, Dictionary<string, string> placeholders, bool isHtml = true);
    Task SendWelcomeEmail(string email, string username, int code, CancellationToken cancellationToken);
    Task ResendConfirmationEmail(string email, string username, int code, CancellationToken cancellationToken);
    Task PasswordResetEmail(string email, string username, CancellationToken cancellationToken);
}