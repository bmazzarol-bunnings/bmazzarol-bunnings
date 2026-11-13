using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WakeTool;

public static class Cron
{
    public static bool IsDue(JobConfig job, DateTime now, DateTime lastRun)
    {
        if (job.Schedule.StartsWith("*/"))
            return (now - lastRun).TotalMinutes >= ParseInterval(job.Schedule[2..]);

        if (job.Schedule.EndsWith('m') && !job.Schedule.Contains(':'))
            return (now - lastRun).TotalMinutes >= int.Parse(job.Schedule.TrimEnd('m'));

        if (job.Schedule.StartsWith("daily "))
        {
            var target = TimeSpan.Parse(job.Schedule[6..]);
            return lastRun.Date < now.Date && now.TimeOfDay >= target;
        }

        return false;
    }

    private static int ParseInterval(string s) => int.Parse(s.TrimEnd('m'));
}

public static class Monitor
{
    public static string RunFingerprint(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c {ShellQuote(command)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(30_000);
            if (p.ExitCode != 0)
                return $"ERR:{p.ExitCode}:{Truncate(stderr, 200)}";
            return Sha256_16(stdout);
        }
        catch (Exception ex)
        {
            return $"EXC:{ex.Message}";
        }
    }

    private static string Sha256_16(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static string ShellQuote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";
}
