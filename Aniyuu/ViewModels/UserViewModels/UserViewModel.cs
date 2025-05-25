namespace Aniyuu.ViewModels.UserViewModels;

public class UserViewModel
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? ProfilePhoto { get; set; }
    public bool? IsActive { get; set; }
    public int? WatchTime { get; set; }
    public string? Gender { get; set; }
    public List<string?> Roles { get; set; }
    public DateTime CreatedAt { get; set; }
}