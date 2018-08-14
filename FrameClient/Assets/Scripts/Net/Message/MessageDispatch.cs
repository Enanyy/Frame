using System;
using System.Collections.Generic;
using PBMessage;
namespace Network
{

    public static class MessageDispatch 
    {
        static Dispatcher mDispatcher = new Dispatcher();

        public static bool RegisterReceiver<T>(MessageID varMessageID, Action<T> action) where T : class
        {
            return mDispatcher.RegisterReceiver((int)varMessageID, action);
        }
        public static void UnRegisterReceiver<T>(MessageID varMessageID, Action<T> action) where T : class
        {
            mDispatcher.UnRegisterReceiver((int)varMessageID, action);
        }

        public static void Dispatch(MessageBuffer message)
        {
            if(message == null)
            {
                return;
            }
            int id = message.id();

            Type type =GetType((MessageID)id);

            if (type == null)
            {
                return;
            }

            object data = ProtoTransfer.DeserializeProtoBuf(message.body(), type);
            if(data ==null)
            {
                return;
            }

            mDispatcher.Dispatch(id, data, type);
        }

        public static Type GetType(MessageID varMessageID)
        {
            return mDispatcher.GetType((int)varMessageID);
        }

        public static void Clear()
        {
            mDispatcher.Clear();
        }
    }
}

