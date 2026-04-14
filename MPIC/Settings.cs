using System.Collections.Generic;

namespace MPIC
{
    public class MailboxSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class MonitoringSettings
    {
        public int MaxTimeToCreateDealAfterLetter { get; set; }
        public List<MailboxSettings> MonitoredMailboxes { get; set; }
    }
}
