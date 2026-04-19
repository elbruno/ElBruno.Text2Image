using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Infrastructure;

/// <summary>
/// Adapter allowing Spectre.Console.Cli to resolve types from Microsoft.Extensions.DependencyInjection.
/// </summary>
internal sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
