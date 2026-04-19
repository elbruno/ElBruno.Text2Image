using Spectre.Console;
using ElBruno.Text2Image.Cli.Providers;

namespace ElBruno.Text2Image.Cli.Tui;

/// <summary>
/// Renders progress updates during image generation.
/// </summary>
internal sealed class ProgressRenderer : IProgress<GenerationProgress>
{
    private readonly ProgressTask _task;

    public ProgressRenderer(ProgressTask task)
    {
        _task = task;
    }

    public void Report(GenerationProgress value)
    {
        _task.Value = value.Step;
        _task.MaxValue = value.TotalSteps;
        if (!string.IsNullOrWhiteSpace(value.Message))
        {
            _task.Description = Markup.Escape(value.Message);
        }
    }

    /// <summary>
    /// Helper to run an async operation with a Spectre.Console progress bar.
    /// </summary>
    public static async Task<T> RunWithProgressAsync<T>(
        string description,
        Func<IProgress<GenerationProgress>, CancellationToken, Task<T>> work,
        CancellationToken ct)
    {
        return await AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(Markup.Escape(description), maxValue: 100);
                var progress = new ProgressRenderer(task);
                return await work(progress, ct);
            });
    }
}
