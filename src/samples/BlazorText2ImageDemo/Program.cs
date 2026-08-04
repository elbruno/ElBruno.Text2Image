using ElBruno.Text2Image.BlazorComponents.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddText2ImageBlazorComponents();
var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<BlazorText2ImageDemo.Components.App>().AddInteractiveServerRenderMode();
app.Run();
