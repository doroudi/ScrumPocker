using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Radzen;
using ScrumPoker.Components;
using ScrumPoker.Data.Dto;
using ScrumPoker.Data.Models;
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
    private readonly List<string> EstimateOptions = ["☕", "1", "2", "3", "5", "8", "13"]; //TODO: move to separated file
    private bool UserIsJoined => ActiveUser != null || IsAdmin;
    private bool IsAdmin => ActiveUser?.Id == ActiveSession?.CreatorId;
    private bool _initializationRequired;

    private ParticipantDto? ActiveUser;

    private string? SelectedValue { get; set; } = null;
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

        if (UserId != ActiveSession.CreatorId)
            _initializationRequired = true;
        else
        {
            await SetAdminUser();
            StateHasChanged();
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
    #endregion

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
        StateHasChanged();
        await jsRuntime.InvokeVoidAsync("setCookie", "UserId", participant.Id);
    }

    private async Task DoEstimate(string value)
    {
        if (ActiveSession == null || ActiveUser == null)
            return;

        if (SelectedValue == value)
            return;
        _ = int.TryParse(value, out int estimatedValue);

        await sessionService.EstimateTaskAsync(ActiveSession.Id, ActiveSession.ActiveBacklog.Id, ActiveUser.Id, estimatedValue);
        await InvokeAsync(() =>
        {
            SelectedValue = value;
            StateHasChanged();
        });
    }

    private async Task SetAdminUser()
    {
        ActiveUser = new ParticipantDto
        {
            Id = ActiveSession.CreatorId,
            DisplayName = "Admin"
        };
    }

    private async Task JoinToSessionSocket(Participant participant, CancellationToken cancellationToken)
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
        await _hubConnection.StartAsync();
    }


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

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("UserLeft");
            await _hubConnection.DisposeAsync();
        }
    }

    private string? UserId =>
        httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c => c.Key == "UserId").Value;
}
