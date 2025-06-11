using DnsClient.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using ScrumPoker.Data.Dto;
using ScrumPoker.Data.Models;
using ScrumPoker.Data.Services;
using System.Collections.Concurrent;

namespace ScrumPoker.Data.Hubs;

public class SessionHub(ILogger<SessionHub> logger, ISessionService sessionService) : Hub
{
    private readonly ConcurrentDictionary<string, string> _connectionToUserMap = [];

    public async Task CreateSession(Session session)
    {
        _connectionToUserMap[Context.ConnectionId] = $"{session.Id}|{session.CreatorId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id.ToString());
        var admin = new Participant { Id = ObjectId.Parse(session.CreatorId), DisplayName = "Administrator" };
        await Clients.Group(session.Id.ToString()).SendAsync("SessionCreated", admin);
    }

    public async Task JoinSession(string sessionId, ParticipantDto participant)
    {
        _connectionToUserMap[Context.ConnectionId] = $"{sessionId}|{participant.Id}";

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Group(sessionId).SendAsync("UserJoined", participant);
    }
    public async Task UserLeft()
    {
        await OnDisconnectedAsync(null);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            if (_connectionToUserMap.TryRemove(Context.ConnectionId, out var sessionParticipant))
            {
                var sessionId = sessionParticipant.Split('|')[0];
                var participantId = sessionParticipant.Split('|')[1];
                var result = await sessionService.RemoveParticipantFromSessionAsync(long.Parse(sessionId), participantId, default);
                if(!result.IsError)
                    await Clients.Group(sessionId).SendAsync("UserLeft", participantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Cannot remove user, {ex.Message}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
