using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class SeasonModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AnimeId { get; set; }

    public int? SeasonNumber { get; set; }
    public string? Title { get; set; }
}