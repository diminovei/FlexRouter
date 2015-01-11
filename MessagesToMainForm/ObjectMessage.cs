namespace FlexRouter.MessagesToMainForm
{
    public class ObjectMessage : IMessage
    {
        public MessageToMainForm MessageType { get; set; }
        public object AnyObject { get; set; }
    }
}