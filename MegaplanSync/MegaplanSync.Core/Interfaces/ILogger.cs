namespace MegaplanSync.Core.Interfaces;

public enum LogLevel
{
    Trace,    // Детальная информация
    Debug,    // Отладочная информация
    Information, // Общая информация о ходе выполнения
    Warning,  // Предупреждения, потенциальные проблемы
    Error,    // Ошибки, которые не приводят к сбою приложения
    Critical  // Критические ошибки, приводящие к сбою или остановке
}

public interface ILogger
{
    /// <summary>
    /// Возвращает полный путь к файлу лога, если он есть.
    /// </summary>
    string? LogFilePath { get; }

    /// <summary>
    /// Универсальный метод для логирования сообщения с заданным уровнем.
    /// </summary>
    /// <param name="level">Уровень логируемого сообщения.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="exception">Исключение, связанное с сообщением (опционально).</param>
    /// <param name="logToFile">Флаг, указывающий, нужно ли записывать сообщение в лог-файл (для вашей реализации). По умолчанию true.</param>
    /// <param name="logToConsole">Флаг, указывающий, нужно ли выводить сообщение в консоль (для вашей реализации). По умолчанию true.</param>
    void Log(LogLevel level, string message, Exception? exception = null, bool logToFile = true, bool logToConsole = true);

    void LogTrace(string message, bool logToFile = true, bool logToConsole = true);
    void LogDebug(string message, bool logToFile = true, bool logToConsole = false);
    void LogInformation(string message, bool logToFile = true, bool logToConsole = true);
    void LogWarning(string message, bool logToFile = true, bool logToConsole = true);
    void LogError(string message, Exception? exception = null, bool logToFile = true, bool logToConsole = false);
    void LogCritical(string message, Exception? exception = null, bool logToFile = true, bool logToConsole = true);

    // Событие для обратной связи (для консольного вывода в приложении)
    event Action<string> OnLogFormattedMessage;
}
