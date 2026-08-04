using ElBruno.Text2Image.BlazorComponents.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Text2Image.BlazorComponents.Extensions;

public static class Text2ImageBlazorComponentsServiceExtensions
{
    public static IServiceCollection AddText2ImageBlazorComponents(this IServiceCollection services)
    {
        services.AddScoped<Text2ImageState>();
        return services;
    }
}
