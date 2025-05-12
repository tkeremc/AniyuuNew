using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class GenreModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? GenreId { get; set; }
    public string? GenreName { get; set; }
    public string? Description { get; set; }
}