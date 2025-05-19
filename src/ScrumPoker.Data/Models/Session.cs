using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ScrumPoker.Data.Models;

public class Session
{
    public Session(Guid creatorId, string? name)
    {
        CreatorId = creatorId;
        DisplayName = name;
    }
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string? DisplayName { get; set; }
    public Guid CreatorId { get; set; }
    public List<Backlog> Backlogs { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public List<Participant> Participants { get; set; } = [];
    [BsonRepresentation(BsonType.ObjectId)]
    public string ActiveTaskId { get; set; }
}
