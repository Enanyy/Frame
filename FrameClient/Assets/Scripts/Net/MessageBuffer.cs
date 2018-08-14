using System;

namespace Network
{
    public class MessageBuffer
    {
        byte[] mBuffer;

        public const int MESSAGE_ID_OFFSET = 0;  //包ID偏移
        public const int MESSAGE_BODY_SIZE_OFFSET = 4; //包体大小偏移
        public const int MESSAGE_VERSION_OFFSET = 8;    //包版本偏移
        public const int MESSAGE_EXTRA_OFFSET = 12;  //额外数据
        public const int MESSAGE_BODY_OFFSET = 16; //包体偏移
        public const int MESSAGE_HEAD_SIZE = 16;//包头大小

        public const int MESSAGE_VERSION = 1;

        public static int MESSAGE_MAX_VALUE = 1000;
        public static int MESSAGE_MIN_VALUE = 0;

        //定义一个静态的包头
        public static byte[] head = new byte[MESSAGE_HEAD_SIZE];
        public static bool IsValid(byte[] buffer)
        {
            if (buffer == null) return false;

            if (buffer.Length < MESSAGE_HEAD_SIZE) return false;

            int messageId = 0;
            if(Decode(buffer, MESSAGE_ID_OFFSET, ref messageId)==false)
            {
                return false;
            }


            int version = 0;
            if(Decode(buffer, MESSAGE_VERSION_OFFSET, ref version) == false)
            {
                return false;
            }

            if (messageId > MESSAGE_MIN_VALUE && messageId < MESSAGE_MAX_VALUE && version == MESSAGE_VERSION)
            {
                return true;
            }
            return false;
        }

        public static bool Decode(byte[] buffer, int offset,ref int value)
        {
            if (buffer == null || buffer.Length < MESSAGE_HEAD_SIZE || offset + 4 > buffer.Length)
            {
                return false;
            }

            value = BitConverter.ToInt32(buffer, offset);

            return true;
        }
        public byte[] buffer
        {
            get
            {
                return mBuffer;
            }
        }

        public int length
        {
            get
            {
                return mBuffer.Length;
            }
        }

        public MessageBuffer(int length)
        {
            mBuffer = new byte[length];
        }

        public MessageBuffer(byte[] data)
        {
            mBuffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, mBuffer, 0, data.Length);
        }

        public MessageBuffer(int messageId, byte[] data, int extra)
        {
            mBuffer = new byte[MESSAGE_HEAD_SIZE + data.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, mBuffer, MESSAGE_ID_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, mBuffer, MESSAGE_BODY_SIZE_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(MESSAGE_VERSION), 0, mBuffer, MESSAGE_VERSION_OFFSET, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(extra), 0, mBuffer, MESSAGE_EXTRA_OFFSET, 4);
            Buffer.BlockCopy(data, 0, mBuffer, MESSAGE_BODY_OFFSET, data.Length);
        }
       
       

        public bool IsValid()
        {
            return IsValid(mBuffer);
        }

        public int id()
        {
            int messageid = -1;

            Decode(mBuffer, MESSAGE_ID_OFFSET, ref messageid);

            return messageid;
        }

        public int version()
        {
            int version = -1;
            Decode(mBuffer, MESSAGE_VERSION_OFFSET, ref version);
            return version;
        }

        public int extra()
        {
            int extra = -1;
            Decode(mBuffer, MESSAGE_EXTRA_OFFSET, ref extra);
            return extra;
        }

        public byte[] body()
        {
            int bodySize = -1;
            if (Decode(mBuffer, MESSAGE_BODY_SIZE_OFFSET, ref bodySize))
            {
                byte[] body = new byte[bodySize];
                Buffer.BlockCopy(mBuffer, MESSAGE_BODY_OFFSET, body, 0, bodySize);
                return body;
            }
            return null;
        }   
    }
}
