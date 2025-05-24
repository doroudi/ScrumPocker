using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(string creatorId, string sessionName);
}
