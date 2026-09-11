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
            // Key: mailbox username. Value: UTC time of the most recent email that was already processed.
            public Dictionary<string, DateTime> LastProcessedEmailTimeUtc { get; set; } = new Dictionary<string, DateTime>();
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

        /// <summary>
        /// Возвращает UTC-время последнего обработанного письма для указанного ящика.
        /// Начальное значение — DateTime.MinValue (ещё не обрабатывали).
        /// </summary>
        public DateTime GetLastProcessedTimeUtc(string mailboxId)
        {
            return _state.LastProcessedEmailTimeUtc.TryGetValue(mailboxId, out var dt) ? dt : DateTime.MinValue;
        }

        /// <summary>
        /// Обновляет время последнего обработанного письма (только если новое время больше).
        /// </summary>
        public void UpdateLastProcessedTimeUtc(string mailboxId, DateTime utcTime)
        {
            if (_state.LastProcessedEmailTimeUtc.TryGetValue(mailboxId, out var existing))
            {
                if (utcTime <= existing)
                    return; // не откатываем время назад
            }
            _state.LastProcessedEmailTimeUtc[mailboxId] = utcTime;
            SaveState();
        }

        /// <summary>
        /// Проверяет, нужно ли отправить уведомление о сбое.
        /// Для писем без Message-ID формирует синтетический ключ на основе email отправителя и времени.
        /// </summary>
        public bool ShouldSendFailureNotification(string mailboxId, string? messageId, string? senderEmail = null, DateTime? receivedUtc = null)
        {
            // Если Message-ID отсутствует — используем составной ключ для дедупликации
            var effectiveId = messageId;
            if (string.IsNullOrEmpty(effectiveId))
            {
                if (!string.IsNullOrEmpty(senderEmail) && receivedUtc.HasValue)
                {
                    // Формируем стабильный синтетический ключ: "__null_{sender}_{date:yyyyMMddHHmm}"
                    effectiveId = $"__null_{senderEmail}_{receivedUtc.Value:yyyyMMddHHmm}";
                }
                else
                {
                    // Не хватает данных для дедупликации — всегда уведомляем (защита от молчания)
                    return true;
                }
            }

            if (_state.NotifiedErrors.TryGetValue(mailboxId, out var notifiedMessages))
            {
                return !notifiedMessages.Contains(effectiveId);
            }

            return true;
        }

        public void RecordFailure(string mailboxId, string? messageId, string? senderEmail = null, DateTime? receivedUtc = null)
        {
            _state.BrokenMailboxes.Add(mailboxId);

            var effectiveId = messageId;
            if (string.IsNullOrEmpty(effectiveId))
            {
                if (!string.IsNullOrEmpty(senderEmail) && receivedUtc.HasValue)
                {
                    effectiveId = $"__null_{senderEmail}_{receivedUtc.Value:yyyyMMddHHmm}";
                }
                else
                {
                    // Если не можем сформировать ключ — не сохраняем, в ShouldSendFailureNotification будет всегда true
                    SaveState();
                    return;
                }
            }

            if (!_state.NotifiedErrors.ContainsKey(mailboxId))
            {
                _state.NotifiedErrors[mailboxId] = new HashSet<string>();
            }
            _state.NotifiedErrors[mailboxId].Add(effectiveId);
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