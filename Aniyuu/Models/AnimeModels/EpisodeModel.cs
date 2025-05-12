using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class EpisodeModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AnimeId { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SeasonId { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }

    public int? EpisodeNumber { get; set; }
    public TimeSpan? Duration { get; set; }

    public string? BunnyVideoGuid { get; set; }

    public string? Thumbnail { get; set; }
    public int? ViewCount { get; set; }

    public DateTime UploadDate { get; set; }
    
    
}