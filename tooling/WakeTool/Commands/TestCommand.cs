using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WakeTool;

public sealed class TestCommand(IAnsiConsole console) : AsyncCommand<TestCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--command <CMD>")]
        [Description("Shell command to run and fingerprint.")]
        public string Command { get; init; } = "echo test";
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var fp = Monitor.RunFingerprint(settings.Command);
            console.WriteLine($"Fingerprint: {fp}");
            await Task.CompletedTask;
            return 0;
        }
        catch (Exception ex)
        {
            console.WriteLine($"Error: {ex}");
            return 1;
        }
    }
}
