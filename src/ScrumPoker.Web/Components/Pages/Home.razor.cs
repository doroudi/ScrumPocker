using Radzen;
using ScrumPoker.Components;

namespace ScrumPoker.Web.Components.Pages
{
    public partial class Home
    {
        async Task JoinSession()
        {
            await DialogService.OpenAsync<JoinSession>("Join a Session", options: new DialogOptions { Width = "400px" });
        }

        async Task CreateNewSession()
        {
            var result = await DialogService.OpenAsync<CreateSession>("Create a new session", options: new DialogOptions { Width = "470px" });
            Console.WriteLine(result);
        }
    }
}
