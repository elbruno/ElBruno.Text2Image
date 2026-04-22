#if NET10_0_OR_GREATER
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Collection definitions to serialize test execution where global state is modified.
/// Tests that call Directory.SetCurrentDirectory() must run serially to avoid 
/// stepping on each other's working directory and temp file cleanup.
/// </summary>

[CollectionDefinition("Global State", DisableParallelization = true)]
public class GlobalStateCollection
{
    // This collection has no code; it's used only to define the collection name and settings.
}
#endif
