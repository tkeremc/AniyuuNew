using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.UserModels;

public class ActivationCodeModel :  BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public int? ActivationCode { get; set; }
    public string? UserId { get; set; }
    public DateTime ExpireAt { get; set; }
    public DateTime UsedAt { get; set; }
    public bool? IsExpired { get; set; } = false;
}