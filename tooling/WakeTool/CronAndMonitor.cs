using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Cronos;

namespace WakeTool;

internal static class Cron
{
    /// <summary>
    /// Due when the next cron occurrence (UTC) strictly after lastRun is <= now.
    /// A job with no recorded lastRun (DateTime.MinValue) is due immediately.
    /// </summary>
    public static bool IsDue(JobConfig job, DateTime now, DateTime lastRun)
    {
        if (lastRun <= DateTime.MinValue)
            return true;

        var expr = CronExpression.Parse(job.Schedule);
        return expr.GetNextOccurrence(lastRun) <= now;
    }
}

internal static class Monitor
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
        return Convert.ToHexString(bytes)[..16];
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

}
