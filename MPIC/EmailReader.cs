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
        /// <returns>An EmailDetails object containing the sender, date, and body of the last email, or null if the inbox is empty or an error occurs.</returns>
        public async Task<EmailDetails?> GetLastEmailDetailsAsync()
        {
            try
            {
                using (var client = new ImapClient())
                {
                    // Connect to the server
                    await client.ConnectAsync(ImapHost, ImapPort, UseSsl);

                    // Authenticate
                    // WARNING: Storing passwords directly in code is insecure. 
                    // Consider using a secure secret management system.
                    await client.AuthenticateAsync(_userName, _password);

                    // Open the inbox
                    var inbox = client.GetFolder(MailboxName);
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
