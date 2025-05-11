using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class AnimeModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string? Title { get; set; }
    public Dictionary<string, string>? AlternativeTitles { get; set; }
    public string? Description { get; set; }
    
    public string? BannerLink { get; set; }
    public string? BackdropLink { get; set; }
    
    public int? EpisodeCount { get; set; }
    public int? SeasonCount { get; set; }
    
    public List<string>? GenreIds { get; set; }
    public List<string>? Tags { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
    public string? Status { get; set; }
    
    // public List<string>? Studios { get; set; }
    
    public int? MALId{ get; set; }
    public double? MALScore { get; set; }
    public int? MALRank { get; set; }
    public string? MALRating { get; set; }
    
    public int? ViewCount { get; set; }
    public int? FavoriteCount { get; set; }
    
    public string? Slug { get; set; }

    public string? BunnyLibraryId { get; set; }
}