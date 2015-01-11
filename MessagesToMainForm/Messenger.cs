using System.Collections.Generic;

namespace FlexRouter.MessagesToMainForm
{
    public static class Messenger
    {
        private static readonly List<IMessage> Messages = new List<IMessage>();
        public static void AddMessage(MessageToMainForm messageType)
        {
            var m = new SimpleMessage {MessageType = messageType};
            AddMessage(m);
        }
        public static void AddMessage(MessageToMainForm messageType, string text)
        {
            var m = new TextMessage { MessageType = messageType, Text = text};
            AddMessage(m);
        }

        public static void AddMessage(MessageToMainForm messageType, object anyObject)
        {
            var m = new ObjectMessage { MessageType = messageType, AnyObject = anyObject };
            AddMessage(m);
        }

        public static void AddMessage(IMessage message)
        {
            lock(Messages)
                Messages.Add(message);
        }
        public static IMessage[] GetMessages()
        {
            IMessage[] tempMessages;
            lock(Messages)
            {
                tempMessages = Messages.ToArray();
                Messages.Clear();
            }
            return tempMessages;
        }
    }
}
