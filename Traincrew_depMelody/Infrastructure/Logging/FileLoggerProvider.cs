using Microsoft.Extensions.Logging;

namespace Traincrew_depMelody.Infrastructure.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string? _currentLogFilePath;

    public FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, this);
    }

    internal void WriteEntry(string message)
    {
        try
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var logFilePath = Path.Combine(_logDirectory, $"app-{today}.log");

            lock (_lock)
            {
                if (_currentLogFilePath != logFilePath)
                {
                    _writer?.Dispose();
                    _writer = null;
                    _writer = new StreamWriter(logFilePath, append: true, System.Text.Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    _currentLogFilePath = logFilePath;
                }

                _writer?.WriteLine(message);
            }
        }
        catch
        {
            // ロギングの失敗はサイレントに無視する
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string categoryName, FileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = logLevel.ToString();
        var message = formatter(state, exception);

        var entry = $"{timestamp} [{levelStr}] {_categoryName}: {message}";
        if (exception != null)
            entry += Environment.NewLine + exception.ToString();

        _provider.WriteEntry(entry);
    }
}
