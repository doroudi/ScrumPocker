using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson.Serialization.Conventions;
using Radzen;
using ScrumPoker.Data.Hubs;
using ScrumPoker.Data.Services;
using ScrumPoker.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddRadzenComponents();

var pack = new ConventionPack { new CamelCaseElementNameConvention() };
ConventionRegistry.Register("CamelCaseConvention", pack, t => true);

//builder.Services.Configure<ScrumPokerDatabaseSettings>(builder.Configuration.GetSection("ScrumPokerDatabase"));
builder.AddMongoDBClient("ScrumPoker");

builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<SessionHub>();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opt =>
{
    opt.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

var app = builder.Build();

app.MapDefaultEndpoints();

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
