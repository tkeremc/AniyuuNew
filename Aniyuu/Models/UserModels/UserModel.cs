using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aniyuu.Models.UserModels;

public class UserModel : BaseModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? ProfilePhoto { get; set; } = "";
    public int? WatchTime { get; set; } = 0;
    public string? Gender { get; set; } = "not set";
    public string? HashedPassword { get; set; }
    public bool? IsActive { get; set; } = true;
    public bool? IsBanned { get; set; }  = false;
    public List<string>? Roles { get; set; } = [("user")];
    public List<string>? Devices { get; set; }
}