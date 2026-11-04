using System.Reflection;

namespace ElBruno.Text2Image.Cli.Commands;

internal static class SkillFileResources
{
    private const string ResourcePrefix = "ElBruno.Text2Image.Cli.SkillFiles.";

    public static IReadOnlyDictionary<string, string>? Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                continue;

            var relativePath = resourceName[ResourcePrefix.Length..]
                .Replace('\\', '/')
                .Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relativePath) ||
                relativePath.Split(Path.DirectorySeparatorChar).Any(segment => segment is "" or "." or ".."))
            {
                throw new InvalidOperationException($"Invalid embedded skill resource path '{relativePath}'.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded skill resource '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            files.Add(relativePath, reader.ReadToEnd());
        }

        return files.ContainsKey("SKILL.md") ? files : null;
    }
}
