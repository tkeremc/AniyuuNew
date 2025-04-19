namespace Aniyuu.Interfaces.UserInterfaces;

public interface IActivationService
{
    Task<bool> ActivateUser(int code, CancellationToken cancellationToken);
    Task<bool> ResendActivationCode(string email, CancellationToken cancellationToken);
    Task<int?> GenerateActivationCode(string email, CancellationToken cancellationToken);
}