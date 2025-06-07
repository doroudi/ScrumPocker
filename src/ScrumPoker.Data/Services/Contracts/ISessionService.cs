using ErrorOr;
using ScrumPoker.Data.Dto;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public interface ISessionService
{
    Task<ErrorOr<SessionDto>> CreateSessionAsync(string sessionName);
    Task<ErrorOr<Success>> EstimateTaskAsync(long sessionId, string backlogId, string participantId, int value);
    Task<ErrorOr<SessionDto>> GetSessionByIdAsync(long id, CancellationToken cancellationToken);
    Task<ErrorOr<ParticipantDto>> JoinSessionAsync(long sessionId, string displayName, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> RemoveParticipantFromSessionAsync(long sessionId, string participantId, CancellationToken cancellationToken);
}
