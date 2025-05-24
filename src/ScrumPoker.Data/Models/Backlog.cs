using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Backlog
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; }
    [BsonElement("title")]
    public string? Title { get; private set; }
    public List<Estimate> Estimates { get; private set; } = [];
    [BsonElement("finalEstimate")]
    public int FinalEstimate { get; private set; }
}
