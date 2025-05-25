
namespace ScrumPoker.Data.Models;
public class ScrumPokerDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string SessionsCollectionName { get; set; } = null!;
}
