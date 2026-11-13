using System.Diagnostics;

namespace WakeTool;

public enum InjectMode
{
    Tmux,
    Connect,
    File
}

public static class Injector
{
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
