using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

namespace MPIC
{
    public class EmailReader
    {
        private const string ImapHost = "imap.yandex.ru";
        private const int ImapPort = 993;
        private const bool UseSsl = true;
        private const string MailboxName = "INBOX";

        private readonly string _userName;
        private readonly string _password;

        public EmailReader(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }

        /// <summary>
        /// Connects to the mailbox and retrieves the details of the most recent email.
        /// </summary>
        /// <returns>An EmailDetails object containing the sender, date, subject and body of the last email, or null if the inbox is empty or an error occurs.</returns>
        public async Task<EmailDetails?> GetLastEmailDetailsAsync()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    await client.ConnectAsync(ImapHost, ImapPort, UseSsl);
                    await client.AuthenticateAsync(_userName, _password);

                    var inbox = client.GetFolder(MailboxName);
                    await inbox.OpenAsync(FolderAccess.ReadOnly);

                    var uids = await inbox.SearchAsync(SearchQuery.All);
                    if (!uids.Any())
                    {
                        Console.WriteLine("The inbox is empty.");
                        return null;
                    }

                    var lastMessageUid = uids.Last();
                    var message = await inbox.GetMessageAsync(lastMessageUid);

                    await client.DisconnectAsync(true);

                    if (message.From.FirstOrDefault() is MailboxAddress sender)
                    {
                        return new EmailDetails
                        {
                            MessageId = message.MessageId,
                            Sender = sender.Address,
                            ReceivedDate = message.Date,
                            Subject = message.Subject,
                            Body = message.TextBody ?? message.HtmlBody
                        };
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching the email: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the most recent emails (up to count) that arrived after <paramref name="afterUtc"/>.
        /// Used to check ALL relevant emails, not just the very last one.
        /// </summary>
        /// <param name="afterUtc">Only emails received after this UTC date are returned. Pass DateTime.MinValue to get the last N emails.</param>
        /// <param name="count">Maximum number of recent emails to fetch.</param>
        /// <returns>List of email details ordered by received date descending (newest first).</returns>
        public async Task<List<EmailDetails>> GetRecentEmailsAsync(DateTime afterUtc, int count = 10)
        {
            var result = new List<EmailDetails>();
            try
            {
                using (var client = new ImapClient())
                {
                    await client.ConnectAsync(ImapHost, ImapPort, UseSsl);
                    await client.AuthenticateAsync(_userName, _password);

                    var inbox = client.GetFolder(MailboxName);
                    await inbox.OpenAsync(FolderAccess.ReadOnly);

                    IList<UniqueId> uids;
                    if (afterUtc > DateTime.MinValue)
                    {
                        // Ищем сообщения, полученные после указанной даты (по дате в заголовке письма).
                        // Отметим: поиск по заголовку отправки менее надёжен, чем по InternalDate, но MailKit
                        // не предоставляет SearchQuery.ReceivedSince в используемой версии — поэтому
                        // дополнительная фильтрация по ReceivedDate выполняется ниже на клиенте.
                        uids = await inbox.SearchAsync(SearchQuery.SentSince(afterUtc));
                    }
                    else
                    {
                        uids = await inbox.SearchAsync(SearchQuery.All);
                    }

                    if (!uids.Any())
                        return result;

                    // Берём последние N писем
                    var recentUids = uids
                        .OrderByDescending(uid => uid)
                        .Take(count)
                        .ToList();

                    foreach (var uid in recentUids)
                    {
                        var message = await inbox.GetMessageAsync(uid);
                        if (message.From.FirstOrDefault() is MailboxAddress sender)
                        {
                            result.Add(new EmailDetails
                            {
                                MessageId = message.MessageId,
                                Sender = sender.Address,
                                ReceivedDate = message.Date,
                                Subject = message.Subject,
                                Body = message.TextBody ?? message.HtmlBody
                            });
                        }
                    }

                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching recent emails: {ex.Message}");
            }

            return result;
        }
    }
}
