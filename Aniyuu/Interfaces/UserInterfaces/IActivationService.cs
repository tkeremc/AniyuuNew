namespace Aniyuu.Interfaces.UserInterfaces;

public interface IActivationService
{
    Task<bool> ActivateUser(string code, CancellationToken cancellationToken);
    Task<bool> ResendActivationCode(string email, CancellationToken cancellationToken);
}