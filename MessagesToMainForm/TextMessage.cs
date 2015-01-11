namespace FlexRouter.MessagesToMainForm
{
    public class TextMessage : IMessage
    {
        public MessageToMainForm MessageType { get; set; }
        public string Text { get; set; }
    }
}