namespace MPIC
{
    public class EmailDetails
    {
        public string? MessageId { get; set; }
        public string? Sender { get; set; }
        public DateTimeOffset ReceivedDate { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }

        /// <summary>
        /// Проверяет, является ли письмо ответом или пересылкой (Re:/Fwd:/R:/Reply).
        /// </summary>
        public bool IsReplyOrForward()
        {
            if (string.IsNullOrWhiteSpace(Subject))
                return false;

            var upper = Subject.TrimStart().ToUpperInvariant();
            return upper.StartsWith("RE:")
                || upper.StartsWith("RE[")
                || upper.StartsWith("FWD:")
                || upper.StartsWith("FW:")
                || upper.StartsWith("R:")
                || upper.StartsWith("R[")
                || upper.StartsWith("REPLY:");
        }
    }
}
