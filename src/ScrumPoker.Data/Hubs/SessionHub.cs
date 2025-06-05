using DnsClient.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ScrumPoker.Data.Models;
using ScrumPoker.Data.Services;
using System.Collections.Concurrent;

namespace ScrumPoker.Data.Hubs;

public class SessionHub(ILogger<SessionHub> logger, ISessionService sessionService) : Hub
{
    private readonly ConcurrentDictionary<string, string> _connectionToUserMap = [];

    public async Task CreateSession(string sessionId, string creatorId)
    {
        _connectionToUserMap[Context.ConnectionId] = $"{sessionId}|{creatorId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Group(sessionId).SendAsync("SessionCreated");
    }

    public async Task JoinSession(string sessionId, Participant participant)
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
                await sessionService.RemoveParticipantFromSessionAsync(sessionId, participantId);
                await Clients.Group(sessionId).SendAsync("UserLeft", participantId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Cannot remove user");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
