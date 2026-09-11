using System.Collections.Generic;

namespace MPIC
{
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
    /// Модель для секции "MpicSettings" в appsettings.json.
    /// </summary>
    public class RootSettings
    {
        // ───── Мегаплан (API) ─────
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BaseApUrl { get; set; } = string.Empty;

        // ───── Мониторинг ─────
        public int MaxTimeToCreateDealAfterLetter { get; set; }
        public int RunIntervalMinutes { get; set; }
        public List<MailboxSettings> MonitoredMailboxes { get; set; } = [];
        public NotificationSettings NotificationSettings { get; set; } = new();

        /// <summary>
        /// Если true, письма с темой, начинающейся на Re:/Fwd:/R: (ответы/пересылки),
        /// не будут вызывать сработки интеграции.
        /// </summary>
        public bool SkipReplyLetters { get; set; }
    }
}