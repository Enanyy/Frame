using System;
namespace Network
{
    public class MessageInfo
    {
        MessageBuffer mMessageBuffer;

        Session mSession;

        public MessageBuffer buffer { get { return mMessageBuffer; } }
        public Session session { get { return mSession; } }

        public MessageInfo(MessageBuffer message, Session c)
        {
            mSession = c;
            mMessageBuffer = message;
        }

        
    }
}
