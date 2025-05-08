using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class AnimeModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? BannerLink { get; set; }
    public string? EpisodeCount { get; set; }
}