using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Radzen;
using ScrumPoker.Data.Hubs;
using ScrumPoker.Data.Models;
using ScrumPoker.Data.Services;
using ScrumPoker.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddRadzenComponents();

builder.Services.Configure<ScrumPokerDatabaseSettings>(builder.Configuration.GetSection("ScrumPokerDatabase"));
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<SessionHub>();
builder.Services.AddSignalR(opt =>
{
    opt.EnableDetailedErrors = true;
});
builder.Services.AddResponseCompression(opt =>
{
    opt.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseResponseCompression();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapBlazorHub().WithOrder(-1);
app.MapHub<SessionHub>("/sessionHub");

app.Run();
