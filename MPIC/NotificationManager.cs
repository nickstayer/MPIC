using System.Text.Json;

namespace MPIC
{
    public class NotificationManager
    {
        private const string StateFilePath = "notification_state.json";
        private NotificationState _state;

        private class NotificationState
        {
            public bool IsIntegrationBroken { get; set; }
            public HashSet<string> NotifiedErrorMessages { get; set; } = new HashSet<string>();
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

        public bool ShouldSendFailureNotification(string messageId)
        {
            if (string.IsNullOrEmpty(messageId)) return true; // Always notify if we can't track the message
            return !_state.NotifiedErrorMessages.Contains(messageId);
        }

        public void RecordFailure(string messageId)
        {
            if (!string.IsNullOrEmpty(messageId))
            {
                _state.NotifiedErrorMessages.Add(messageId);
            }
            _state.IsIntegrationBroken = true;
            SaveState();
        }

        public bool ShouldSendSuccessNotification()
        {
            return _state.IsIntegrationBroken;
        }

        public void RecordSuccess()
        {
            _state.IsIntegrationBroken = false;
            _state.NotifiedErrorMessages.Clear();
            SaveState();
        }
    }
}
