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
            var psi = new ProcessStartInfo("/bin/bash", ["-c", command])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // RunAndCaptureText drains stdout and stderr concurrently — no pipe-buffer deadlock.
            ProcessTextOutput result = Process.RunAndCaptureText(psi, TimeSpan.FromMilliseconds(30_000));
            if (result.ExitStatus.ExitCode != 0)
                return $"ERR:{result.ExitStatus.ExitCode}:{Truncate(result.StandardError, 200)}";
            return Sha256_16(result.StandardOutput);
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

}
