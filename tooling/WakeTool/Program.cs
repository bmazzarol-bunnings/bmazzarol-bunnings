using Bunnings.Common.Spectre;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using WakeTool;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);

var app = new CommandApp(new ServiceCollectionTypeRegistrar(services));
app.Configure(configurator =>
{
    configurator
        .SetApplicationName("waketool")
        .AddCommand<RunCommand>("run")
        .WithDescription("Start the wake loop (monitors + cron ticks).")
        .WithExample("run", "--mode", "tmux", "--target", "%0");
    configurator
        .AddCommand<JobsCommand>("jobs")
        .WithDescription("List configured jobs and their state.");
    configurator
        .AddCommand<TestCommand>("test")
        .WithDescription("Run a single monitor command and print its fingerprint.")
        .WithExample("test", "--command", "gh pr list --json number,state");
});

return await app.RunAsync(args);
