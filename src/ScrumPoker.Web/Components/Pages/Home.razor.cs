using Radzen;
using ScrumPoker.Components;

namespace ScrumPoker.Web.Components.Pages;

public partial class Home(DialogService DialogService)
{
    async Task CreateNewSession()
    {
        await DialogService.OpenAsync<CreateSession>("Create a new session", options: new DialogOptions { Width = "470px" });
    }

    async Task JoinSession()
    {
        await DialogService.OpenAsync<JoinSession>("Join a Session", options: new DialogOptions { Width = "400px" });
    }
}
