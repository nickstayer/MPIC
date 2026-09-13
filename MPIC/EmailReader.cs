using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;

namespace MPIC
{
    /// <summary>
    /// Читает последний EmailDetails из почтового ящика.
    /// Параметры IMAP-подключения берутся из конфигурации (секция MPIC:Imap).
    /// </summary>
    public class EmailReader
    {
        private readonly ImapSettings _imap;
        private readonly string _userName;
        private readonly string _password;

        public EmailReader(string userName, string password, ImapSettings imap)
        {
            _userName = userName;
            _password = password;
            _imap = imap ?? new ImapSettings();
        }

        public EmailReader(string userName, string password)
        {
            _userName = userName;
            _password = password;
            _imap = new ImapSettings();
        }

        /// <summary>
        /// Connects to the mailbox and retrieves the details of the most recent email.
        /// </summary>
        /// <returns>An EmailDetails object containing the sender, date, and body of the last email, or null if the inbox is empty or an error occurs.</returns>
        public async Task<EmailDetails?> GetLastEmailDetailsAsync()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    // Connect to the server
                    await client.ConnectAsync(_imap.Host, _imap.Port, _imap.UseSsl);

                    // Authenticate
                    await client.AuthenticateAsync(_userName, _password);

                    // Open the inbox
                    var inbox = client.GetFolder(_imap.MailboxName);
                    await inbox.OpenAsync(FolderAccess.ReadOnly);

                    // Search for all messages and get the UID of the last one
                    var uids = await inbox.SearchAsync(SearchQuery.All);
                    if (!uids.Any())
                    {
                        Console.WriteLine("The inbox is empty.");
                        return null;
                    }

                    var lastMessageUid = uids.Last();

                    // Fetch the full message
                    var message = await inbox.GetMessageAsync(lastMessageUid);

                    // Disconnect from the server
                    await client.DisconnectAsync(true);

                    // Get the sender's address
                    if (message.From.FirstOrDefault() is MailboxAddress sender)
                    {
                        return new EmailDetails
                        {
                            MessageId = message.MessageId,
                            Sender = sender.Address,
                            ReceivedDate = message.Date,
                            Body = message.TextBody ?? message.HtmlBody
                        };
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                // In a real application, use a proper logging framework
                Console.WriteLine($"An error occurred while fetching the email: {ex.Message}");
                return null;
            }
        }
    }
}
