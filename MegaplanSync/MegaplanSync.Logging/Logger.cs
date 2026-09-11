using MegaplanSync.Core.Interfaces;

namespace MegaplanSync.Logging;

public class Logger : ILogger
{
    private static volatile Logger? _instance;
    private static readonly object _lock = new();

    public string LogFilePath { get; private set; }

    public event Action<string> OnLogFormattedMessage;

    private Logger(string logFile)
    {
        LogFilePath = logFile;
        try
        {
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }
        catch (Exception ex)
        {
            OnLogFormattedMessage?.Invoke($"ОШИБКА (Логгер): Не удалось создать директорию для логов: {ex.Message}");
        }
    }

    public static void Initialize(string baseLogDirectory = "Logs", string fileNamePrefix = "app_")
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    var currentPath = AppDomain.CurrentDomain.BaseDirectory;
                    var logsDirFullName = Path.Combine(currentPath, baseLogDirectory);
                    var today = DateTime.Today.ToString("yyyyMMdd");
                    var logFile = Path.Combine(logsDirFullName, $"{fileNamePrefix}{today}.log");

                    _instance = new Logger(logFile);
                    _instance.OnLogFormattedMessage?.Invoke($"[INFO] {DateTime.Now:HH:mm:ss}: Логгер инициализирован. Файл лога: {_instance.LogFilePath}");
                }
            }
        }
        else
        {
            _instance.OnLogFormattedMessage?.Invoke($"[WARN] {DateTime.Now:HH:mm:ss}: Логгер уже инициализирован.");
        }
    }

    public static ILogger Instance
    {
        get
        {
            if (_instance == null)
            {
                Initialize();
            }
            return _instance!;
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null, bool logToFile = true, bool logToConsole = true)
    {
        string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToString().ToUpperInvariant()}]: {message}";
        if (exception != null)
        {
            formattedMessage += $"{Environment.NewLine}  Исключение: {exception.GetType().Name} - {exception.Message}{Environment.NewLine}{exception.StackTrace}";
        }

        if (logToFile)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                OnLogFormattedMessage?.Invoke($"[CRITICAL] {DateTime.Now:HH:mm:ss}: ОШИБКА ЗАПИСИ В ЛОГ-ФАЙЛ '{LogFilePath}': {ex.Message}");
            }
        }

        if (logToConsole)
        {
            OnLogFormattedMessage?.Invoke(formattedMessage);
        }
    }

    public void LogTrace(string message, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Trace, message, logToFile: logToFile, logToConsole: logToConsole);
    public void LogDebug(string message, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Debug, message, logToFile: logToFile, logToConsole: logToConsole);
    public void LogInformation(string message, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Information, message, logToFile: logToFile, logToConsole: logToConsole);
    public void LogWarning(string message, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Warning, message, logToFile: logToFile, logToConsole: logToConsole);
    public void LogError(string message, Exception? exception = null, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Error, message, exception, logToFile: logToFile, logToConsole: logToConsole);
    public void LogCritical(string message, Exception? exception = null, bool logToFile = true, bool logToConsole = true) =>
        Log(LogLevel.Critical, message, exception, logToFile: logToFile, logToConsole: logToConsole);


    public void Chapter()
    {
        string separator = new string('-', 25);
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, separator + Environment.NewLine + Environment.NewLine);
            }
            OnLogFormattedMessage?.Invoke(separator);
        }
        catch (Exception ex)
        {
            OnLogFormattedMessage?.Invoke($"[ERROR] {DateTime.Now:HH:mm:ss}: ОШИБКА записи разделителя в лог-файл '{LogFilePath}': {ex.Message}");
        }
    }

    public void Paragraph()
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, Environment.NewLine);
            }
            OnLogFormattedMessage?.Invoke(string.Empty);
        }
        catch (Exception ex)
        {
            OnLogFormattedMessage?.Invoke($"[ERROR] {DateTime.Now:HH:mm:ss}: ОШИБКА записи абзаца в лог-файл '{LogFilePath}': {ex.Message}");
        }
    }
}
