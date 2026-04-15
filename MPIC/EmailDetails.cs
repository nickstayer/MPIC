namespace MPIC
{
    public class EmailDetails
    {
        public string? MessageId { get; set; }
        public string? Sender { get; set; }
        public DateTimeOffset ReceivedDate { get; set; }
        public string? Body { get; set; }
    }
}
