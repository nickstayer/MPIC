namespace MPIC;

/// <summary>
/// Почтовый ящик, monitored app'ом.
/// </summary>
public class MailboxSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class NotificationSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool UseSsl { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
}

/// <summary>
/// Настройки подключения к IMAP-серверу для чтения писем.
/// </summary>
public class ImapSettings
{
    public string Host { get; set; } = "imap.yandex.ru";
    public int Port { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public string MailboxName { get; set; } = "INBOX";
}

/// <summary>
/// Настройки MPIC. Читаются через IConfiguration из секции <see cref="SectionName"/>.
/// Источники: appsettings.json → user secrets → переменные окружения.
/// </summary>
public class RootSettings
{
    public const string SectionName = "MPIC";

    // Настройки API Мегаплана
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseApUrl { get; set; } = string.Empty;

    // Настройки мониторинга
    public int MaxTimeToCreateDealAfterLetter { get; set; }
    public int RunIntervalMinutes { get; set; }
    public List<MailboxSettings> MonitoredMailboxes { get; set; } = [];

    // Настройки почтовых уведомлений
    public NotificationSettings NotificationSettings { get; set; } = new();

    // Настройки IMAP для чтения писем
    public ImapSettings Imap { get; set; } = new();
}
