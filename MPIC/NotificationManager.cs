using System.Text.Json;

namespace MPIC
{
    public class NotificationManager
    {
        private const string StateFilePath = "notification_state.json";
        private NotificationState _state;

        private class NotificationState
        {
            // Set of mailbox usernames that are currently considered broken.
            public HashSet<string> BrokenMailboxes { get; set; } = new HashSet<string>();
            // Key: mailbox username. Value: Set of message-IDs for which notifications have been sent.
            public Dictionary<string, HashSet<string>> NotifiedErrors { get; set; } = new Dictionary<string, HashSet<string>>();
        }

        public NotificationManager()
        {
            LoadState();
        }

        private void LoadState()
        {
            if (File.Exists(StateFilePath))
            {
                try
                {
                    var json = File.ReadAllText(StateFilePath);
                    _state = JsonSerializer.Deserialize<NotificationState>(json) ?? new NotificationState();
                }
                catch
                {
                    _state = new NotificationState();
                }
            }
            else
            {
                _state = new NotificationState();
            }
        }

        private void SaveState()
        {
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StateFilePath, json);
        }

        public bool ShouldSendFailureNotification(string mailboxId, string messageId)
        {
            // Always notify if we can't track the message
            if (string.IsNullOrEmpty(messageId)) return true; 

            // If we have records for this mailbox, check if we've already notified for this message
            if (_state.NotifiedErrors.TryGetValue(mailboxId, out var notifiedMessages))
            {
                return !notifiedMessages.Contains(messageId);
            }

            // No records for this mailbox yet, so we should definitely notify
            return true;
        }

        public void RecordFailure(string mailboxId, string messageId)
        {
            _state.BrokenMailboxes.Add(mailboxId);

            if (!string.IsNullOrEmpty(messageId))
            {
                if (!_state.NotifiedErrors.ContainsKey(mailboxId))
                {
                    _state.NotifiedErrors[mailboxId] = new HashSet<string>();
                }
                _state.NotifiedErrors[mailboxId].Add(messageId);
            }
            
            SaveState();
        }

        public bool ShouldSendSuccessNotification(string mailboxId)
        {
            return _state.BrokenMailboxes.Contains(mailboxId);
        }

        public void RecordSuccess(string mailboxId)
        {
            _state.BrokenMailboxes.Remove(mailboxId);
            // Clean up the error message history for the recovered mailbox
            if (_state.NotifiedErrors.ContainsKey(mailboxId))
            {
                _state.NotifiedErrors.Remove(mailboxId);
            }
            SaveState();
        }
    }
}
