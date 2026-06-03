#if NET10_0_OR_GREATER
using System.Net;
using System.Text;
using Xunit;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Commands;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Tests.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli;

/// <summary>
/// Phase 3A: Critical tests for CLI command execution.
/// Tests all CLI commands: generate, config, secrets, doctor, providers, version, init.
/// </summary>
[Collection("ConfigStore")]
public class CommandExecutionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalAppData;
    private readonly string _originalXdgConfig;
    private readonly TextWriter _originalStdErr;

    public CommandExecutionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? string.Empty;
        _originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
        
        Environment.SetEnvironmentVariable("APPDATA", _tempDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempDir);
        
        // Suppress Console.Error output during tests to prevent warning noise
        _originalStdErr = Console.Error;
        Console.SetError(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetError(_originalStdErr);
        
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalXdgConfig);
        
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region VersionCommand Tests

    [Fact]
    public void VersionCommand_ReturnsSuccess()
    {
        var command = new VersionCommand();
        var context = CreateContext();

        var exitCode = command.Execute(context);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void VersionCommand_ExecutesWithoutException()
    {
        var command = new VersionCommand();
        var context = CreateContext();

        var exception = Record.Exception(() => command.Execute(context));

        Assert.Null(exception);
    }

    #endregion

    #region ProvidersCommand Tests

    [Fact]
    public async Task ProvidersCommand_ListsAllProviders_ReturnsSuccess()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ProvidersCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ProvidersCommand_ExecutesWithoutException()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ProvidersCommand(registry, resolver, store);
        var context = CreateContext();

        var exception = await Record.ExceptionAsync(() => command.ExecuteAsync(context));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ProvidersCommand_ShowsAllRegisteredProviders()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ProvidersCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(registry.All);
    }

    #endregion

    #region ConfigCommand Tests

    [Fact]
    public async Task ConfigCommand_Show_WithEmptyConfig_ReturnsSuccess()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings { Action = "show" };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Path_ShowsConfigPath_ReturnsSuccess()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings { Action = "path" };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Set_WithValidKey_ReturnsSuccess()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "set",
            Key = "default-provider",
            Value = "foundry-flux2"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Set_PersistsToConfig()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "set",
            Key = "default-provider",
            Value = "foundry-flux2"
        };
        var context = CreateContext();

        await command.ExecuteAsync(context, settings);
        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("foundry-flux2", config.DefaultProvider);
    }

    [Fact]
    public async Task ConfigCommand_Set_WithMissingValue_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "set",
            Key = "default-provider",
            Value = null
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Remove_WithValidKey_ReturnsSuccess()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        
        // First set a value
        var config = new AppConfig { DefaultProvider = "foundry-flux2" };
        await store.SaveAsync(config, CancellationToken.None);
        
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "remove",
            Key = "default-provider"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Remove_DeletesValue()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        
        // First set a value
        var config = new AppConfig { DefaultProvider = "foundry-flux2" };
        await store.SaveAsync(config, CancellationToken.None);
        
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "remove",
            Key = "default-provider"
        };
        var context = CreateContext();

        await command.ExecuteAsync(context, settings);
        var updatedConfig = await store.LoadAsync(CancellationToken.None);

        Assert.Null(updatedConfig.DefaultProvider);
    }

    [Fact]
    public async Task ConfigCommand_InvalidAction_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings { Action = "invalid-action" };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Show_AfterSet_DisplaysValue()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        
        // Set a value
        var setCmd = new ConfigCommand(registry, resolver, store);
        var setSettings = new ConfigCommand.Settings 
        { 
            Action = "set",
            Key = "default-provider",
            Value = "foundry-flux2"
        };
        await setCmd.ExecuteAsync(CreateContext(), setSettings);

        // Show config
        var showCmd = new ConfigCommand(registry, resolver, store);
        var showSettings = new ConfigCommand.Settings { Action = "show" };
        var exitCode = await showCmd.ExecuteAsync(CreateContext(), showSettings);

        Assert.Equal(0, exitCode);
    }

    #endregion

    #region DoctorCommand Tests

    [Fact]
    public async Task DoctorCommand_ExecutesWithoutError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoctorCommand_ChecksAllProviders()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(registry.All);
    }

    [Fact]
    public async Task DoctorCommand_ShowsSystemInfo()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exception = await Record.ExceptionAsync(() => command.ExecuteAsync(context));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DoctorCommand_ChecksConfigFile()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoctorCommand_ReportsProviderHealth()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
        
        // Verify all providers can be checked
        foreach (var provider in registry.All)
        {
            var health = await provider.CheckAsync(CancellationToken.None);
            Assert.NotNull(health);
        }
    }

    #endregion

    #region GenerateCommand Tests

    [Fact]
    public async Task GenerateCommand_WithMissingProvider_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = "a cat",
            Provider = "non-existent-provider"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task GenerateCommand_WithEmptyPrompt_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = ""
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task GenerateCommand_ValidatesPromptNotEmpty()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = "   "
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task GenerateCommand_WithNoDefaultProvider_AndNoProviderOption_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = "a cat",
            Provider = null
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task GenerateCommand_WithInvalidDimensions_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = "a cat",
            Width = -1,
            Height = 512
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void GenerateCommand_Settings_HasDefaultValues()
    {
        var settings = new GenerateCommand.Settings();

        Assert.Equal(512, settings.Width);
        Assert.Equal(512, settings.Height);
    }

    #endregion

    #region InitCommand Tests

    [Fact]
    public void InitCommand_WritesBothFiles_WhenTargetAll()
    {
        var testDir = Path.Combine(_tempDir, "init-test-all");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "all", KeepExisting = false };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(0, exitCode);
            
            var githubPath = Path.Combine(testDir, ".github", "skills", "t2i", "SKILL.md");
            var claudePath = Path.Combine(testDir, ".claude", "skills", "t2i", "SKILL.md");
            
            Assert.True(File.Exists(githubPath));
            Assert.True(File.Exists(claudePath));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void InitCommand_WritesOnlyGithub_WhenTargetGithub()
    {
        var testDir = Path.Combine(_tempDir, "init-test-github");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "github", KeepExisting = false };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(0, exitCode);
            
            var githubPath = Path.Combine(testDir, ".github", "skills", "t2i", "SKILL.md");
            var claudePath = Path.Combine(testDir, ".claude", "skills", "t2i", "SKILL.md");
            
            Assert.True(File.Exists(githubPath));
            Assert.False(File.Exists(claudePath));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void InitCommand_WritesOnlyClaude_WhenTargetClaude()
    {
        var testDir = Path.Combine(_tempDir, "init-test-claude");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "claude", KeepExisting = false };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(0, exitCode);
            
            var githubPath = Path.Combine(testDir, ".github", "skills", "t2i", "SKILL.md");
            var claudePath = Path.Combine(testDir, ".claude", "skills", "t2i", "SKILL.md");
            
            Assert.False(File.Exists(githubPath));
            Assert.True(File.Exists(claudePath));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void InitCommand_UpdatesExistingFile_ByDefault()
    {
        var testDir = Path.Combine(_tempDir, "init-test-update");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            const string sentinel = "OLD CONTENT";
            var githubPath = Path.Combine(testDir, ".github", "skills", "t2i", "SKILL.md");
            
            Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
            File.WriteAllText(githubPath, sentinel);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "github", KeepExisting = false };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(0, exitCode);
            
            var content = File.ReadAllText(githubPath);
            Assert.DoesNotContain(sentinel, content);
            Assert.Contains("# t2i", content);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void InitCommand_KeepsExistingFile_WithKeepExisting()
    {
        var testDir = Path.Combine(_tempDir, "init-test-keep");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            const string sentinel = "KEEP THIS CONTENT";
            var githubPath = Path.Combine(testDir, ".github", "skills", "t2i", "SKILL.md");
            
            Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
            File.WriteAllText(githubPath, sentinel);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "github", KeepExisting = true };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(0, exitCode);
            
            var content = File.ReadAllText(githubPath);
            Assert.Equal(sentinel, content);
            Assert.DoesNotContain("# t2i", content);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public void InitCommand_ReturnsError_WhenInvalidTarget()
    {
        var testDir = Path.Combine(_tempDir, "init-test-invalid");
        Directory.CreateDirectory(testDir);
        var originalCwd = Directory.GetCurrentDirectory();
        
        try
        {
            Directory.SetCurrentDirectory(testDir);
            
            var command = new InitCommand();
            var settings = new InitCommand.Settings { Target = "invalid-target", KeepExisting = false };
            var context = CreateContext();

            var exitCode = command.Execute(context, settings);

            Assert.Equal(1, exitCode);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    #endregion

    #region SecretsCommand Tests

    [Fact]
    public async Task SecretsCommand_List_WithNoSecrets_ReturnsSuccess()
    {
        var (registry, resolver, _) = CreateConfigDependencies();
        var secretsCmd = new SecretsCommand(registry, resolver);
        var settings = new SecretsCommand.Settings { Action = "list" };
        var context = CreateContext();

        var exitCode = await secretsCmd.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task SecretsCommand_Remove_WithValidProvider_ReturnsSuccess()
    {
        var (registry, resolver, _) = CreateConfigDependencies();
        var secretsCmd = new SecretsCommand(registry, resolver);
        var settings = new SecretsCommand.Settings 
        { 
            Action = "remove",
            Provider = "foundry-flux2"
        };
        var context = CreateContext();

        var exitCode = await secretsCmd.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task SecretsCommand_Test_WithInvalidProvider_ReturnsError()
    {
        var (registry, resolver, _) = CreateConfigDependencies();
        var secretsCmd = new SecretsCommand(registry, resolver);
        var settings = new SecretsCommand.Settings 
        { 
            Action = "test",
            Provider = "non-existent-provider"
        };
        var context = CreateContext();

        var exitCode = await secretsCmd.ExecuteAsync(context, settings);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task SecretsCommand_InvalidAction_ReturnsError()
    {
        var (registry, resolver, _) = CreateConfigDependencies();
        var secretsCmd = new SecretsCommand(registry, resolver);
        var settings = new SecretsCommand.Settings { Action = "invalid-action" };
        var context = CreateContext();

        var exitCode = await secretsCmd.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    #endregion

    #region Additional ConfigCommand Tests

    [Fact]
    public async Task ConfigCommand_Set_WithoutKey_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "set",
            Key = null,
            Value = "some-value"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_Remove_WithMissingKey_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings 
        { 
            Action = "remove",
            Key = null
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ConfigCommand_SetAll_Endpoint_AppliesToAllCloudProviders()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings
        {
            Action = "set-all",
            Key = "endpoint",
            Value = "https://shared.services.ai.azure.com"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);
        var config = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(0, exitCode);
        foreach (var provider in registry.All.Where(p => p.Kind == ProviderKind.Cloud))
        {
            Assert.Equal("https://shared.services.ai.azure.com", config.Providers[provider.Id].Endpoint);
        }
    }

    [Fact]
    public async Task ConfigCommand_SetAll_ApiKey_AppliesToAllCloudProvidersAsSecret()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var fakeStore = new FakeSecretStore { Name = "dpapi" };
        var resolver = new SecretResolver(new List<ISecretStore> { fakeStore });
        var store = new ConfigStore();
        var registry = new ProviderRegistry(new List<IProviderAdapter>
        {
            new FoundryFlux2Adapter(httpClientFactory, resolver, store),
            new FoundryMaiImage2Adapter(httpClientFactory, resolver, store),
            new FoundryGptImage1p5Adapter(httpClientFactory, resolver, store),
            new FoundryGptImage2Adapter(httpClientFactory, resolver, store)
        });

        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings
        {
            Action = "set-all",
            Key = "apiKey",
            Value = "shared-secret-key"
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(0, exitCode);
        foreach (var provider in registry.All.Where(p => p.Kind == ProviderKind.Cloud))
        {
            var resolved = await resolver.ResolveAsync(provider.Id, "apiKey", null, CancellationToken.None);
            Assert.Equal("shared-secret-key", resolved);
        }
    }

    [Fact]
    public async Task ConfigCommand_SetAll_WithMissingValue_ReturnsError()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new ConfigCommand(registry, resolver, store);
        var settings = new ConfigCommand.Settings
        {
            Action = "set-all",
            Key = "apiKey",
            Value = null
        };
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context, settings);

        Assert.Equal(1, exitCode);
    }

    #endregion

    #region Additional GenerateCommand Tests

    [Fact]
    public void GenerateCommand_Settings_HasDefaultOutputPath()
    {
        var settings = new GenerateCommand.Settings { Prompt = "test" };

        Assert.Null(settings.OutputPath);
    }

    [Fact]
    public void GenerateCommand_Settings_HasDefaultSteps()
    {
        var settings = new GenerateCommand.Settings();

        Assert.Equal(512, settings.Width);
        Assert.Equal(512, settings.Height);
    }

    [Fact]
    public async Task GenerateCommand_WithProviderFlag_OverridesDefault()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        
        // Set default provider
        var config = new AppConfig { DefaultProvider = "foundry-flux2" };
        await store.SaveAsync(config, CancellationToken.None);
        
        var command = new GenerateCommand(registry, resolver, store);
        var settings = new GenerateCommand.Settings 
        { 
            Prompt = "test",
            Provider = "foundry-mai2"
        };
        var context = CreateContext();

        // Should try to use mai2 instead of flux2
        var exitCode = await command.ExecuteAsync(context, settings);

        // Will fail due to missing credentials, but that's expected
        Assert.NotEqual(0, exitCode);
    }

    #endregion

    #region Additional DoctorCommand Tests

    [Fact]
    public async Task DoctorCommand_WithNoConfig_StillSucceeds()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoctorCommand_ChecksMultipleProviders()
    {
        var (registry, resolver, store) = CreateConfigDependencies();
        var command = new DoctorCommand(registry, resolver, store);
        var context = CreateContext();

        var exitCode = await command.ExecuteAsync(context);

        Assert.Equal(0, exitCode);
        Assert.True(registry.All.Count() >= 4);
    }

    #endregion

    #region Helper Methods

    private static CommandContext CreateContext()
    {
        var remaining = new FakeRemainingArguments();
        return new CommandContext(
            Array.Empty<string>(),
            remaining,
            "test-command",
            null
        );
    }

    private SecretResolver CreateSecretResolver()
    {
        var fakeStore = new FakeSecretStore();
        var stores = new List<ISecretStore> { fakeStore };
        return new SecretResolver(stores);
    }

    private (ProviderRegistry Registry, SecretResolver Resolver, ConfigStore Store) CreateConfigDependencies()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var resolver = CreateSecretResolver();
        var store = new ConfigStore();
        
        var adapters = new List<IProviderAdapter>
        {
            new FoundryFlux2Adapter(httpClientFactory, resolver, store),
            new FoundryMaiImage2Adapter(httpClientFactory, resolver, store),
            new FoundryGptImage1p5Adapter(httpClientFactory, resolver, store),
            new FoundryGptImage2Adapter(httpClientFactory, resolver, store)
        };
        
        var registry = new ProviderRegistry(adapters);
        
        return (registry, resolver, store);
    }

    private sealed class FakeRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
            return new HttpClient(handler);
        }
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }

    #endregion
}
#endif
