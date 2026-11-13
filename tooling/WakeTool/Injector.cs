using System.Diagnostics;

namespace WakeTool;

public enum InjectMode
{
    Tmux,
    Connect,
    File,
    Herdr
}

public static class Injector
{
    public static InjectMode ParseMode(string s) => s.Trim().ToLowerInvariant() switch
    {
        "tmux" => InjectMode.Tmux,
        "connect" => InjectMode.Connect,
        "file" => InjectMode.File,
        "herdr" => InjectMode.Herdr,
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
