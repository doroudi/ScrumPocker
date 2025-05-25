using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ScrumPoker.Components;
using ScrumPoker.Data.Models;
using ScrumPoker.Data.Services;

namespace ScrumPoker.Web.Components.Pages;

public partial class PokerSession(ISessionService sessionService, DialogService dialogService, IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime, NavigationManager navigation)
{
    [Parameter]
    public required string Id { get; set; }
    public Session? ActiveSession { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadSession(default);
        await base.OnInitializedAsync();
        //var result = await dialogService.OpenAsync<EntranceDialog>("Enter Name", options: new DialogOptions { Width = "470px" });
       // await UpdateUserName(result);
    }

    public async Task LoadSession(CancellationToken cancellationToken)
    {
        if (!long.TryParse(Id, out var numericId))
            throw new Exception("Id is not valid");

        var sessionResult = await sessionService.GetSessionByIdAsync(numericId, cancellationToken);
        if (sessionResult.IsError)
            throw new Exception("");

        ActiveSession = sessionResult.Value;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
        {
            var userId = httpContextAccessor.HttpContext?.Request.Cookies.FirstOrDefault(c => c.Key == "UserId").Value ?? throw new Exception("");
            if (ActiveSession?.CreatorId != userId)
                await UpdateUserName();
        }
        //return base.OnAfterRenderAsync(firstRender);
    }

    public async Task UpdateUserName()
    {
        var result = await dialogService.OpenAsync<EntranceDialog>(
            title: "Enter Name",
            options: new DialogOptions { Width = "470px" });

        if (result != null)
        {
            await sessionService.UpdateUserNameAsync((long)ActiveSession.Id, result);
        }
    }
}
