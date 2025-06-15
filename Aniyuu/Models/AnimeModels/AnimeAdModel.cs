using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.AnimeModels;

public class AnimeAdModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? BackdropLink { get; set; }
    public int? MALId { get; set; }
    public List<MalGenre>? Genres { get; set; }
    public string? LogoLink { get; set; }
    public bool IsActive { get; set; } = true;
}