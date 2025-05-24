using Microsoft.AspNetCore.Components;

namespace ScrumPoker.Web.Components.Pages
{
    public partial class Session
    {
        [Parameter]
        public required string Id { get; set; }
    }
}
