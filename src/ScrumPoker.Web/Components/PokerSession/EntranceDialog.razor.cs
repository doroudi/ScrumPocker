using Microsoft.AspNetCore.Components;
using Radzen;

namespace ScrumPoker.Components;

public partial class EntranceDialog(DialogService dialogService, NavigationManager navigationManager)
{

    class EntranceModel
    {
        public string? DisplayName { get; set; }
    }

    EntranceModel model = new();
    public void Enter()
    {
        //TODO: do validation and show error
        if (!string.IsNullOrEmpty(model.DisplayName))
        {
            dialogService.Close(model.DisplayName);
        }
    }

    public void Cancel()
    {
        dialogService.Close();
        navigationManager.NavigateTo("/");
    }
}
