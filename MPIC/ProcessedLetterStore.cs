using System.Text.Json;
using System.Text.Json.Serialization;

namespace MPIC;

/// <summary>
/// Лёгкое файловое хранилище информации об обработанных письмах.
/// Выбрано вместо встраиваемой БД (SQLite/LiteDB): объёмы — единицы записей на ящик,
/// нет запросов, нужна только устойчивость к сбоям питания и отсутствие зависимостей.
/// </summary>
public class ProcessedLetterStore
{
    private const string StateFilePath = "processed_letters.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _filePath;
    private readonly object _sync = new();
    private ProcessedLettersState _state;

    public ProcessedLetterStore(string? filePath = null)
    {
        // Состояние живёт рядом с исполняемым файлом, а не в текущей директории службы
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, StateFilePath);
        _state = Load();
    }

    public class ProcessedLettersState
    {
        // Ключ — логин ящика; значение — последнее обработанное письмо.
        public Dictionary<string, ProcessedLetterEntry> Mailboxes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ProcessedLetterEntry
    {
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }

        [JsonPropertyName("received_utc")]
        public DateTime? ReceivedUtc { get; set; }

        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        [JsonPropertyName("outcome")]
        public string? Outcome { get; set; }

        [JsonPropertyName("processed_utc")]
        public DateTime? ProcessedUtc { get; set; }
    }

    /// <summary>
    /// Возвращает запись о последнем обработанном письме ящика или null.
    /// </summary>
    public ProcessedLetterEntry? GetLastProcessed(string mailboxUsername)
    {
        lock (_sync)
        {
            return _state.Mailboxes.TryGetValue(mailboxUsername, out var entry)
                ? entry
                : null;
        }
    }

    /// <summary>
    /// Письмо уже обработано? Сравнение по Message-ID (надёжно), а при его отсутствии —
    /// по дате получения (фолбэк, как в ReadNewLetter).
    /// </summary>
    public bool IsAlreadyProcessed(string mailboxUsername, string? messageId, DateTimeOffset receivedDate)
    {
        var last = GetLastProcessed(mailboxUsername);
        if (last == null)
            return false;

        if (!string.IsNullOrEmpty(messageId))
            return !string.IsNullOrEmpty(last.MessageId)
                && string.Equals(last.MessageId, messageId, StringComparison.OrdinalIgnoreCase);

        return last.ReceivedUtc.HasValue
            && Math.Abs((last.ReceivedUtc.Value - receivedDate.ToUniversalTime()).TotalSeconds) < 1.0;
    }

    /// <summary>
    /// Фиксирует письмо как обработанное (перезаписывает запись последнего письма ящика).
    /// Атомарная запись: сначала во временный файл, затем File.Move.
    /// </summary>
    public void MarkProcessed(string mailboxUsername, EmailDetails letter, string outcome)
    {
        var entry = new ProcessedLetterEntry
        {
            MessageId = letter.MessageId,
            ReceivedUtc = letter.ReceivedDate.ToUniversalTime().UtcDateTime,
            Sender = letter.Sender,
            Outcome = outcome,
            ProcessedUtc = DateTime.UtcNow
        };

        lock (_sync)
        {
            _state.Mailboxes[mailboxUsername] = entry;
            Save();
        }
    }

    private ProcessedLettersState Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new ProcessedLettersState();

            var json = File.ReadAllText(_filePath);
            var state = JsonSerializer.Deserialize<ProcessedLettersState>(json, SerializerOptions);
            if (state != null && state.Mailboxes.Comparer != StringComparer.OrdinalIgnoreCase)
                state.Mailboxes = new Dictionary<string, ProcessedLetterEntry>(
                    state.Mailboxes, StringComparer.OrdinalIgnoreCase);
            return state ?? new ProcessedLettersState();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️Не удалось прочитать {StateFilePath}: {ex.Message}. Считаю письма необработанными.");
            return new ProcessedLettersState();
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(_state, SerializerOptions));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            // Падать нельзя: отсутствие файла состояния лишь приведёт к повторной проверке
            Console.WriteLine($"⚠️Не удалось сохранить {StateFilePath}: {ex.Message}");
        }
    }
}
