using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class TcpService : TcpClient
    {
        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();
        private IPEndPoint mServerAdress;

        private Client mService;
        private int mPort;

        private int mSock;

        private Thread mReceiveThread, mSendThread, mActiveThread;

        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnect;
        public event OnExceptionHandler onException;

        public TcpService(Client service):base()
        {
            mService = service;
        }

        public bool IsConnected { get { return Client!=null && Active && Connected; } }

        public new bool Connect(string ip, int port)
        {
            if (IsConnected)
            {
                return true;
            }

            mServerAdress = new IPEndPoint(IPAddress.Parse(ip), port);

            Thread connect = new Thread(ConnectThread);
            connect.Start();

            return true;
        }

        public void Send(MessageBuffer message)
        {
            if (message == null)
            {
                return;
            }
            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        void ConnectThread()
        {
            try
            {
                base.Connect(mServerAdress);

                if (IsConnected == false)
                {
                    Close();
                    return;
                }
                
                mReceiveThread = new Thread(ReceiveThread);
                mSendThread = new Thread(SendThread);
                mActiveThread = new Thread(ActiveThread);


                mReceiveThread.Start();
                mSendThread.Start();
                mActiveThread.Start();

                if (onConnect != null)
                {
                    onConnect();
                }
            }
            catch (Exception e)
            {
                Close();
                throw e;
            }

        }

        public new void Close()
        {
            if (IsConnected)
            {
                GetStream().Close();
            }
            base.Close();

            if (mReceiveThread != null)
            {
                mReceiveThread.Abort();
                mReceiveThread = null;
            }
            if (mSendThread != null)
            {
                mSendThread.Abort();

                mSendThread = null;
            }
            if (onDisconnect != null)
            {
                onDisconnect();
                onDisconnect = null;
            }

        }

        void SendThread()
        {
            while (IsConnected)
            {
                try
                {
                    lock (mSendMessageQueue)
                    {
                        for (int i = 0; i < mSendMessageQueue.Count; ++i)
                        {
                            MessageBuffer message = mSendMessageQueue.Dequeue();

                            if (message == null) continue;

                            GetStream().Write(message.buffer, 0, message.length);
                        }

                        mSendMessageQueue.Clear();
                    }
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 心跳检查
        /// </summary>
        void ActiveThread()
        {
            while (IsConnected)
            {
                Thread.Sleep(1000);
            }

            Close();
        }

        void ReceiveThread()
        {
            while (IsConnected)
            {
                try
                {
                  
                    int receiveSize = Client.Receive(MessageBuffer.head, MessageBuffer.MESSAGE_HEAD_SIZE, SocketFlags.None);
                    if (receiveSize == 0)
                    {
                        return;
                    }

                    if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE)
                    {
                        return;
                    }
                    int messageId = BitConverter.ToInt32(MessageBuffer.head, MessageBuffer.MESSAGE_ID_OFFSET);
                    int bodySize = BitConverter.ToInt32(MessageBuffer.head, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET);

                    if (MessageBuffer.IsValid(MessageBuffer.head) == false)
                    {
                        return;
                    }

                    byte[] messageBuffer = new byte[MessageBuffer.MESSAGE_HEAD_SIZE + bodySize];
                    Array.Copy(MessageBuffer.head, 0, messageBuffer, 0, MessageBuffer.head.Length);

                    if (bodySize > 0)
                    {
                        int receiveBodySize = Client.Receive(messageBuffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

                        if (receiveBodySize != bodySize)
                        {
                            return;
                        }
                    }
                    if (onMessage != null)
                    {
                        onMessage(new MessageBuffer(messageBuffer));
                    }
                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(1);
            }
        }
    }
}