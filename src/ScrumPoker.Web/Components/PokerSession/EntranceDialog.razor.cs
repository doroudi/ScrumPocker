using Microsoft.AspNetCore.Components;
using Radzen;
using ScrumPoker.Data.Services;

namespace ScrumPoker.Components;

public partial class EntranceDialog(ISessionService sessionService, DialogService dialogService, NavigationManager navigationManager)
{
    public string? DisplayName { get; set; }

    public async Task Enter()
    {
        //TODO: do validation and show error
        if (!string.IsNullOrEmpty(DisplayName))
        {
            dialogService.Close(DisplayName);
        }
    }

    public void Cancel()
    {
        dialogService.Close();
        navigationManager.NavigateTo("/");
    }
}
