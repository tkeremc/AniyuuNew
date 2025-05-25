using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class StudioModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int? StudioId { get; set; }
    public string? StudioName { get; set; }
}