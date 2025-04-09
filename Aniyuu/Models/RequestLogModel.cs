using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models;

public class RequestLogModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Method { get; set; }
    public string? Path { get; set; }
    public int StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
}