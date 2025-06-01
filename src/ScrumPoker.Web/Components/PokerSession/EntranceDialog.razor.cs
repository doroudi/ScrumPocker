using Microsoft.AspNetCore.Components;
using Radzen;

namespace ScrumPoker.Components;

public partial class EntranceDialog(DialogService dialogService, NavigationManager navigationManager)
{
    public string? DisplayName { get; set; }

    public void Enter()
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
