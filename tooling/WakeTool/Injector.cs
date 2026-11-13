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
        var escaped = text.Replace("'", "'\\''");
        var psi = new ProcessStartInfo
        {
            FileName = "tmux",
            Arguments = $"send-keys -t {pane} '{escaped}' Enter",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit(5_000);
        return p.ExitCode == 0;
    }

    private static bool ConnectSend(string sessionId, string prompt)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "copilot",
            Arguments = $"--connect {sessionId} -p {ShellQuote(prompt)}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi)!;
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit(120_000);
        return p.ExitCode == 0;
    }

    private static bool HerdrSend(string target, string prompt)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "herdr",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // Target and prompt as separate argv; ArgumentList prevents any shell interpretation of the prompt.
        psi.ArgumentList.Add("agent");
        psi.ArgumentList.Add("prompt");
        psi.ArgumentList.Add(target);
        psi.ArgumentList.Add(prompt);
        psi.ArgumentList.Add("--wait");
        psi.ArgumentList.Add("--timeout");
        psi.ArgumentList.Add("120000");
        using var p = Process.Start(psi)!;
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit(130_000);
        return p.ExitCode == 0;
    }

    private static bool FileWrite(string path, string prompt)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, prompt + "\n");
        return true;
    }

    private static string ShellQuote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";
}
