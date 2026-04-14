using System.Collections.Generic;

namespace MPIC
{
    public class MailboxSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class NotificationSettings
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
        public string RecipientEmail { get; set; }
    }

    public class RootSettings
    {
        // Properties that were in AppSettings
        public string[] LaunchTime { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string BaseApUrl { get; set; }
        public string ConnectionString { get; set; }

        // Properties that were in MonitoringSettings
        public int MaxTimeToCreateDealAfterLetter { get; set; }
        public List<MailboxSettings> MonitoredMailboxes { get; set; }

        // Nested notification settings
        public NotificationSettings NotificationSettings { get; set; }
    }
}
