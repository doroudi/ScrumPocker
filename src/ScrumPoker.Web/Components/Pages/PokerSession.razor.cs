using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Radzen;
using ScrumPoker.Components;
using ScrumPoker.Data.Models;
using ScrumPoker.Data.Services;
using System.Diagnostics;

namespace ScrumPoker.Web.Components.Pages;

public partial class PokerSession(
    ISessionService sessionService,
    DialogService dialogService,
    IHttpContextAccessor httpContextAccessor,
    IJSRuntime jsRuntime,
    NavigationManager navigation) : IAsyncDisposable
{

    #region  Fields
    private Session? ActiveSession;
    private Backlog ActiveBacklog = new();
    private HubConnection? _hubConnection;
    private List<Estimate> Estimates = [];
    #endregion

    #region Parameters
    [Parameter]
    public required string Id { get; set; }
    #endregion

    #region Hooks
    protected override async Task OnInitializedAsync()
    {
        await InitializeWebSocket();
        await LoadSession(default);
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            var userId = httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c => c.Key == "UserId").Value;
            if (userId == null)
                await JoinUserToSession(default);
        }
    }
    #endregion

    public async Task LoadSession(CancellationToken cancellationToken)
    {
        if (!long.TryParse(Id, out var numericId))
            throw new Exception("Id is not valid");

        var sessionResult = await sessionService.GetSessionByIdAsync(numericId, cancellationToken);
        if (sessionResult.IsError)
            throw new Exception("");

        ActiveSession = sessionResult.Value;
        
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
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            await _hubConnection.InvokeAsync("JoinSession", ActiveSession?.Id.ToString(), participant , cancellationToken: cancellationToken);

        //await _hubConnection.SendAsync("UserJoined", ActiveSession?.Id, cancellationToken: cancellationToken);
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
            .ConfigureLogging(logging => {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();


        _hubConnection.On<Participant>("UserJoined", async (participant) =>
        {
            await InvokeAsync(() =>
            {
                if (!ActiveSession.Participants.Any(p => p.Id == participant.Id))
                {
                    ActiveSession.Participants.Add(participant);
                    StateHasChanged();
                }
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
}
