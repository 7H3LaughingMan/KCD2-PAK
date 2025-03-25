using Microsoft.Extensions.Logging;

namespace KCD2_PAK;

public class FileLogger : ILogger
{
    private readonly StreamWriter _logFileWriter;

    public FileLogger(string path) => _logFileWriter = new StreamWriter(File.Open(path, FileMode.Create));

    public FileLogger(FileInfo fileInfo) : this(fileInfo.FullName) { }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logFileWriter.WriteLine(formatter(state, exception));
        _logFileWriter.Flush();
    }
}
