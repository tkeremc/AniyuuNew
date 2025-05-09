namespace Aniyuu.Interfaces.UserInterfaces;

public interface ICurrentUserService
{
    string GetUserId();
    string GetUsername();
    string GetEmail();
    List<string> GetRoles();
    public string GetIpAddress();
    public string GetDeviceId();
    public string GetBrowserData();
    public string GetOSData();
    public string GetUserAddress();
}