namespace Aniyuu.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmail(string email, string username, int code, CancellationToken cancellationToken);
    Task ResendConfirmationEmail(string email, string username, int code, CancellationToken cancellationToken);
    Task PasswordResetEmail(string email, string username, string token, CancellationToken cancellationToken);
    Task NewDeviceLoginEmail(string email, string username, CancellationToken cancellationToken);
}