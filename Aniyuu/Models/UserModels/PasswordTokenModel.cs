using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.UserModels;

public class PasswordTokenModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public string? UserId { get; set; }
    public string? PasswordToken { get; set; }
    public DateTime Expiration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool? IsUsed { get; set; }  = false;
}