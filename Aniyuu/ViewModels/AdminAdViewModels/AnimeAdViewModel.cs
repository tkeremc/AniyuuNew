namespace Aniyuu.ViewModels.AdminAdViewModels;

public class AnimeAdViewModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? BackdropLink { get; set; }
    public int? MALId { get; set; }
    public List<MalGenre>? Genres { get; set; }
    public string? LogoLink { get; set; }
    public bool IsActive { get; set; }
}