using MailKit.Net.Smtp;
using MimeKit;
using MegaplanSync.Core.Interfaces;
using System.Threading.Tasks;

namespace MPIC
{
    public class EmailService
    {
        private readonly NotificationSettings _settings;
        private readonly ILogger _logger;

        public EmailService(NotificationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string subject, string body)
        {
            if (_settings == null)
            {
                _logger.LogError("Настройки для отправки уведомлений не заданы. Отправка невозможна.");
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("MPIC Notifier", _settings.SenderEmail));
                message.To.Add(new MailboxAddress("", _settings.RecipientEmail));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl);
                    await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    _logger.LogInformation($"Уведомление успешно отправлено на {_settings.RecipientEmail}");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Ошибка при отправке email-уведомления: {ex.ToString()}");
            }
        }
    }
}
