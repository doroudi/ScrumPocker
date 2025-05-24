using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ScrumPoker.Data.Models;

public class Session
{
    public required ObjectId Id { get; set; }

    [BsonElement("displayName")]
    public string? DisplayName { get; set; }
    
    [BsonElement("creatorId")]
    public string CreatorId { get; set; }
    
    [BsonElement("backlogs")]
    public List<Backlog> Backlogs { get; set; } = [];
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; set; }
    
    [BsonElement("participants")]
    public List<Participant> Participants { get; set; } = [];
    
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("activeTaskId")]
    public string ActiveTaskId { get; set; }
}
