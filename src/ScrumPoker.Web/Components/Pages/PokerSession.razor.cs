﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Radzen;
using ScrumPoker.Components;
using ScrumPoker.Data.Dto;
using ScrumPoker.Data.Dto.Backlogs;
using ScrumPoker.Data.Services;

namespace ScrumPoker.Web.Components.Pages;

public partial class PokerSession(
    ISessionService sessionService,
    DialogService dialogService,
    IHttpContextAccessor httpContextAccessor,
    IJSRuntime jsRuntime,
    NavigationManager navigation,
    NotificationService notificationService) : IAsyncDisposable
{

    #region  Fields
    private SessionDto? ActiveSession;
    private HubConnection? _hubConnection;
    private bool UserIsJoined => ActiveUser != null || IsAdmin;
    private bool IsAdmin => ActiveUser?.Id == ActiveSession?.CreatorId;
    private bool _initializationRequired;
    private ParticipantDto? ActiveUser;
    private string? UserId =>
       httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c => c.Key == "UserId").Value;

    #endregion!

    #region Parameters
    [Parameter]
    public required string Id { get; set; }
    #endregion

    #region Hooks
    protected override async Task OnInitializedAsync()
    {
        await InitializeWebSocket();
        ActiveSession = await LoadSession(default);

        if (ActiveSession.Participants.Any(x => x.Id == UserId))
        {
            return;
        }
            
        if (UserId != ActiveSession.CreatorId)
            _initializationRequired = true;
        else
        {
            await SetAdminUser();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_initializationRequired)
        {
            if (!UserIsJoined)
            {
                _initializationRequired = false;
                await JoinUserToSession(default);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("UserLeft");
            await _hubConnection.DisposeAsync();
        }
    }
    #endregion

    #region SessionManagement
    public async Task<SessionDto> LoadSession(CancellationToken cancellationToken)
    {
        if (ActiveSession != null)
            return ActiveSession;

        if (!long.TryParse(Id, out var numericId))
            throw new Exception("Id is not valid");

        var sessionResult = await sessionService.GetSessionByIdAsync(numericId, cancellationToken);

        if (sessionResult.IsError)
            throw new Exception("");

        return sessionResult.Value;
    }

    public async Task JoinUserToSession(CancellationToken cancellationToken)
    {
        var displayName = await dialogService.OpenAsync<EntranceDialog>(
            title: "Enter Name",
            options: new DialogOptions { Width = "470px" });

        if (string.IsNullOrEmpty(displayName))
            throw new Exception("Display name cannot be empty");

        if (ActiveSession == null)
            throw new Exception("Session is not loaded");

        var result = await sessionService.JoinSessionAsync(ActiveSession.Id, displayName.ToString(), cancellationToken);
        if (result.IsError)
            return;

        var participant = result.Value as ParticipantDto;
        ActiveUser = participant;
        await jsRuntime.InvokeVoidAsync("setCookie", "UserId", participant.Id);
        await JoinToSessionSocket(ActiveUser, cancellationToken);
        ActiveSession.Participants.Add(participant);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetAdminUser()
    {
        ActiveUser = new ParticipantDto
        {
            Id = ActiveSession.CreatorId,
            DisplayName = "Admin"
        };

        await JoinToSessionSocket(ActiveUser, default);
    }
    #endregion

    #region Estimation
    private async Task DoEstimate(string value)
    {
        if (ActiveSession == null || ActiveUser == null)
            return;

        _ = int.TryParse(value, out int estimatedValue);

        await sessionService.EstimateTaskAsync(ActiveSession.Id, ActiveSession.ActiveBacklog.Id, ActiveUser.Id, estimatedValue);
        await InvokeAsync(StateHasChanged);
    }

    private void DeleteEstimates()
    {
        
    }

    private async Task RevealResults()
    {
        await sessionService.RevealResultsAsync(ActiveSession.Id, default);
    }
    #endregion

    #region WebSocket
    private async Task JoinToSessionSocket(ParticipantDto participant, CancellationToken cancellationToken)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            await _hubConnection.InvokeAsync("JoinSession", ActiveSession?.Id.ToString(), participant, cancellationToken: cancellationToken);
    }

    private async Task InitializeWebSocket()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("/sessionHub"))
            .WithAutomaticReconnect([
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            ])
            .Build();

        _hubConnection.On<ParticipantDto>("ParticipantJoined", async (participant) =>
        {
            if(ActiveSession == null)
                return;

            if (ActiveSession.Participants.Any(p => p.Id == participant.Id))
                return;
                
            await InvokeAsync(() =>
            {
                ActiveSession.Participants.Add(participant);
                StateHasChanged();
            });

            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "User Joined",
                Detail = $"{participant.DisplayName} has joined the session.",
                Duration = 3000
            });
        });



        _hubConnection.On<string>("ParticipantLeft", userId =>
        {
            InvokeAsync(() =>
            {
                var participant = ActiveSession?.Participants.FirstOrDefault(p => p.Id == userId);
                if (participant != null)
                {
                    ActiveSession?.Participants.Remove(participant);

                    notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Participant Left",
                        Detail = $"{participant.DisplayName} has disconnected.",
                        Duration = 3000
                    });
                    StateHasChanged();
                }
            });
        });

        _hubConnection.On<EstimateDto>("TaskEstimated", (estimate) =>
        {
            var existingEstimate = ActiveSession.ActiveBacklog.Estimates.Find(e => e.ParticipantId == estimate.ParticipantId);
            if(existingEstimate != null)
            {
                existingEstimate.Value = estimate.Value;
            }
            else
            {
                ActiveSession.ActiveBacklog.Estimates.Add(estimate);
            }
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        });

        _hubConnection.On<BacklogEstimateSummaryDto>("EstimationRevealed", (result) =>
        {
            InvokeAsync(() =>
            {
                ActiveSession.ActiveBacklog.Estimates.ForEach(e => e.Value = result.Estimates.FirstOrDefault(x => x.ParticipantId == e.ParticipantId)?.Value);
                ActiveSession.ActiveBacklog.Estimates.Sort((x, y) => x.Value?.CompareTo(y.Value) ?? 0);
                ActiveSession.ActiveBacklog.IsRevealed = true;
                StateHasChanged();
            });
        });
        await _hubConnection.StartAsync();
    }
    #endregion

    #region Utilities
    public async Task Share()
    {
        var url = navigation.Uri.ToString();
        await jsRuntime.InvokeVoidAsync("eval",
        @"
            const textArea = document.createElement('textarea');
            textArea.value = '" + url + @"';
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
        ");

        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Link Copied",
            Detail = "Session link has been copied to clipboard.",
            Duration = 3000
        });
    }

    #endregion
}
