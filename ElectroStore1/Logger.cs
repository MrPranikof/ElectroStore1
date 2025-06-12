using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

public static class Logger
{
    private static readonly object _lock = new object();
    private static readonly string _logDirectory;
    private static string _currentLogFile;
    private static readonly Timer _dateCheckTimer;

    static Logger()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(_logDirectory);

        UpdateLogFileName();

        _dateCheckTimer = new Timer(_ => UpdateLogFileName(), null, TimeSpan.Zero, TimeSpan.FromHours(1));
    }

    private static void UpdateLogFileName()
    {
        _currentLogFile = Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
    }

    public static void Info(string message, [CallerMemberName] string caller = "")
    {
        Write("INFO", message, caller);
    }

    public static void Warning(string message, [CallerMemberName] string caller = "")
    {
        Write("WARN", message, caller);
    }

    public static void Error(string message, Exception ex = null, [CallerMemberName] string caller = "")
    {
        if (ex != null)
        {
            Write("ERROR", $"{message} | Exception: {ex.GetType().Name}: {ex.Message}", caller);
            Write("ERROR", $"StackTrace: {ex.StackTrace}", caller);
        }
        else
        {
            Write("ERROR", message, caller);
        }
    }

    public static void Debug(string message, [CallerMemberName] string caller = "")
    {
#if DEBUG
        Write("DEBUG", message, caller);
#endif
    }

    private static void Write(string level, string message, string caller)
    {
        try
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {level,-5} | {caller,-20} | {message}{Environment.NewLine}";

            lock (_lock)
            {
                File.AppendAllText(_currentLogFile, logEntry, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    public static void WriteSeparator(string title = null)
    {
        var separator = new string('=', 80);
        if (!string.IsNullOrEmpty(title))
        {
            Write("INFO", $"{separator} {title} {separator}", "SEPARATOR");
        }
        else
        {
            Write("INFO", separator, "SEPARATOR");
        }
    }
}