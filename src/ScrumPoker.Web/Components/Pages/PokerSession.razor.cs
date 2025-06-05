using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Radzen;
using ScrumPoker.Components;
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
    private Session? ActiveSession;
    private Backlog ActiveBacklog = new();
    private HubConnection? _hubConnection;
    private List<Estimate> Estimates = [];
    private readonly List<string> EstimateOptions = ["☕", "1", "2", "3", "5", "8", "13"];
    private bool userIsJoined;
    private bool IsAdmin => UserId == ActiveSession?.CreatorId;

    private string? SelectedValue { get; set; } = null;
    #endregion

    #region Parameters
    [Parameter]
    public required string Id { get; set; }
    #endregion

    private void UpdatedSelected(string value)
    {
        if (SelectedValue != value)
        {
            InvokeAsync(() =>
            {
                SelectedValue = value;
                StateHasChanged();
            });
        }
    }
    #region Hooks
    protected override async Task OnInitializedAsync()
    {
        await InitializeWebSocket();
        ActiveSession = await LoadSession(default);
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        if (UserId is null)
        {
            await JoinUserToSession(default);
            return;
        }
        ActiveSession = await LoadSession(default);
        if (UserId == ActiveSession.CreatorId && _hubConnection != null)
        {
            await _hubConnection.InvokeAsync("CreateSession", ActiveSession.Id.ToString(), UserId);
        }
        await base.OnAfterRenderAsync(firstRender);
    }
    #endregion

    public async Task<Session> LoadSession(CancellationToken cancellationToken)
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
        var username = await dialogService.OpenAsync<EntranceDialog>(
            title: "Enter Name",
            options: new DialogOptions { Width = "470px" });

        if (string.IsNullOrEmpty(username))
            throw new Exception("");

        if (ActiveSession == null || ActiveSession.Id == null)
            throw new Exception("");

        var result = await sessionService.JoinSessionAsync(ActiveSession.Id.Value, username.ToString(), cancellationToken);
        if (result.IsError)
            return;

        var participant = result.Value as Participant;
        //TODO: switch to session storage instead of cookie
        await jsRuntime.InvokeVoidAsync("setCookie", "UserId", participant.Id);
        await JoinToSessionSocket(participant, cancellationToken);
        userIsJoined = true;
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


        _hubConnection.On<Participant>("UserJoined", async (participant) =>
        {
            await InvokeAsync(() =>
            {
                if (!ActiveSession.Participants.Any(p => p.Id == participant.Id))
                {
                    ActiveSession.Participants.Add(participant);
                    StateHasChanged();
                    notificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "User Joined",
                        Detail = $"{participant.DisplayName} has joined the session.",
                        Duration = 3000
                    });
                }
            });
        });

        _hubConnection.On("SessionCreated", () =>
        {
            InvokeAsync(() =>
            {
                userIsJoined = true;
                StateHasChanged();
            });
        });

        _hubConnection.On<string>("UserLeft", userId =>
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

        _hubConnection.On<List<Estimate>>("Estimate", (estimates) =>
        {
            Estimates = estimates;
        });
        await _hubConnection.StartAsync();
    }


    public async Task Share()
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = "Link Copied",
            Detail = "Session link has been copied to clipboard.",
            Duration = 3000
        });
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
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.InvokeAsync("UserLeft");
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }

    private string? UserId => httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c => c.Key == "UserId").Value;
}
