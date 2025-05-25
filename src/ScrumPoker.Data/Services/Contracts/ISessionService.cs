using ErrorOr;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(string creatorId, string sessionName);
    Task<ErrorOr<string>> JoinSessionAsync(long sessionId, CancellationToken cancellationToken);
    Task<ErrorOr<Session>> GetSessionByIdAsync(long id, CancellationToken cancellationToken);
    ValueTask<ErrorOr<Success>> UpdateUserNameAsync(long sessionId, string userName);
}
