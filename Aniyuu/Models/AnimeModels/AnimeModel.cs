using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class AnimeModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string? Title { get; set; }
    public MalAlternativeTitles? AlternativeTitles { get; set; }
    public string? Description { get; set; }
    
    public string? BannerLink { get; set; }
    public string? BackdropLink { get; set; }
    
    public int? EpisodeCount { get; set; }
    public int? SeasonCount { get; set; }
    public List<int>? Seasons { get; set; }
    
    public int? SeasonNumber { get; set; }
    
    public List<MalGenre>? Genre { get; set; }
    public List<string>? Tags { get; set; }
    public string? Source { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
    public string? Status { get; set; }
    
    public List<MalStudio>? Studios { get; set; }
    
    public int? MALId{ get; set; }
    public double? MALScore { get; set; }
    public int? MALRank { get; set; }
    public string? MALRating { get; set; }
    
    public int? ViewCount { get; set; }
    public int? FavoriteCount { get; set; }
    
    public string? Slug { get; set; }
    
    public List<string>? TrailerLinks { get; set; }
    public bool IsActive { get; set; }
    
    
    [BsonIgnoreIfDefault]
    [BsonElement("score")]
    [BsonRepresentation(BsonType.Double)]
    public double SearchScore { get; set; }

}