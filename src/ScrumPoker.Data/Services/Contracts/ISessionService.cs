using ErrorOr;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public interface ISessionService
{
    Task<ErrorOr<Session>> CreateSessionAsync(string creatorId, string sessionName);
    Task<ErrorOr<Session>> GetSessionByIdAsync(long id, CancellationToken cancellationToken);
    Task<ErrorOr<Participant>> JoinSessionAsync(long sessionId, string displayName, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> RemoveParticipantFromSessionAsync(string sessionId, string participantId);
}
