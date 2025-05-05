namespace Aniyuu.ViewModels;

public class EmailMessageViewModel
{
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? TemplateName { get; set; }
    public Dictionary<string, string>? UsedPlaceholders { get; set; }
}