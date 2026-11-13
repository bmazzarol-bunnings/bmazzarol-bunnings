using System.Diagnostics;
using System.Text.Json;

namespace WakeTool;

internal enum InjectMode
{
    Tmux,
    Connect,
    File,
    Herdr
}

internal enum HerdrInjectResult
{
    Sent,
    Skipped,
    Failed
}

internal static class Injector
{
    private static readonly TimeSpan HerdrListTimeout = TimeSpan.FromSeconds(10);

    public static InjectMode ParseMode(string s) => s.Trim().ToUpperInvariant() switch
    {
        "TMUX" => InjectMode.Tmux,
        "CONNECT" => InjectMode.Connect,
        "FILE" => InjectMode.File,
        "HERDR" => InjectMode.Herdr,
        _ => throw new ArgumentException($"Unknown inject mode '{s}'. Use tmux, connect, file, or herdr.")
    };

    public static bool Send(string prompt, InjectMode mode, string target)
    {
        try
        {
            switch (mode)
            {
                case InjectMode.Tmux:
                    return TmuxSendKeys(target, prompt);
                case InjectMode.Connect:
                    return ConnectSend(target, prompt);
                case InjectMode.File:
                    return FileWrite(target, prompt);
                case InjectMode.Herdr:
                    return HerdrSend(target, prompt);
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[inject] {mode} failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Input-safe herdr injection: before sending, the target agent's lifecycle
    /// state is checked via `herdr agent list`. Only idle/done agents receive
    /// input — a prompt written into a busy prompt box would clobber pending
    /// input. Skipped injections are not treated as sent, so the next tick
    /// retries.
    /// </summary>
    public static HerdrInjectResult HerdrSendSafe(string target, string prompt)
    {
        if (!TryGetAgentState(target, out string? state))
        {
            Console.Error.WriteLine($"[herdr] agent {target} unreachable or not found, injection skipped, retry next tick");
            return HerdrInjectResult.Skipped;
        }
        if (state is not ("idle" or "done"))
        {
            Console.Error.WriteLine($"[herdr] agent {target} {state}, injection skipped, retry next tick");
            return HerdrInjectResult.Skipped;
        }
        return HerdrSend(target, prompt) ? HerdrInjectResult.Sent : HerdrInjectResult.Failed;
    }

    private static bool TryGetAgentState(string target, out string? state)
    {
        state = null;
        try
        {
            var psi = new ProcessStartInfo("herdr", ["agent", "list"])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            // RunAndCaptureText drains stdout and stderr concurrently — no pipe-buffer deadlock.
            ProcessTextOutput result = Process.RunAndCaptureText(psi, HerdrListTimeout);
            if (result.ExitStatus.ExitCode != 0)
            {
                Console.Error.WriteLine($"[herdr] agent list failed (exit {result.ExitStatus.ExitCode}): {result.StandardError.Trim()}");
                return false;
            }
            using var doc = JsonDocument.Parse(result.StandardOutput);
            if (doc.RootElement.TryGetProperty("result", out var res)
                && res.TryGetProperty("agents", out var agents)
                && agents.ValueKind == JsonValueKind.Array)
            {
                foreach (var agent in agents.EnumerateArray())
                {
                    if (agent.TryGetProperty("name", out var name)
                        && name.GetString() is { } agentName
                        && string.Equals(agentName, target, StringComparison.OrdinalIgnoreCase)
                        && agent.TryGetProperty("agent_status", out var status))
                    {
                        state = status.GetString();
                        return true;
                    }
                }
            }
            Console.Error.WriteLine($"[herdr] agent {target} not found in agent list");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[herdr] agent list failed: {ex.Message}");
            return false;
        }
    }

    private static bool TmuxSendKeys(string pane, string text)
    {
        // ArgumentList passes the text as one argv entry — no shell, no quoting.
        var psi = new ProcessStartInfo("tmux", ["send-keys", "-t", pane, text, "Enter"])
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        ProcessExitStatus status = Process.Run(psi, TimeSpan.FromMilliseconds(5_000));
        return status.ExitCode == 0;
    }

    private static bool ConnectSend(string sessionId, string prompt)
    {
        var psi = new ProcessStartInfo("copilot", ["--connect", sessionId, "-p", prompt])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // RunAndCaptureText drains stdout and stderr concurrently — no pipe-buffer deadlock.
        ProcessTextOutput result = Process.RunAndCaptureText(psi, TimeSpan.FromMilliseconds(120_000));
        return result.ExitStatus.ExitCode == 0;
    }

    private static bool HerdrSend(string target, string prompt)
    {
        var psi = new ProcessStartInfo("herdr", ["agent", "prompt", target, prompt, "--wait", "--timeout", "120000"])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // RunAndCaptureText drains stdout and stderr concurrently — no pipe-buffer deadlock.
        ProcessTextOutput result = Process.RunAndCaptureText(psi, TimeSpan.FromMilliseconds(130_000));
        return result.ExitStatus.ExitCode == 0;
    }

    private static bool FileWrite(string path, string prompt)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, prompt + "\n");
        return true;
    }

}
