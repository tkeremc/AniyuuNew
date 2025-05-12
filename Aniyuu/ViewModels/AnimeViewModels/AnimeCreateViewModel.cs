namespace Aniyuu.ViewModels.AnimeViewModels;

public class AnimeCreateViewModel
{
    public int MalId { get; set; }
    public string? BackdropLink { get; set; } = "not set";
    public List<string> Tags { get; set; } = ["not set"];
    public List<string> Trailers { get; set; } = ["not set"];
}