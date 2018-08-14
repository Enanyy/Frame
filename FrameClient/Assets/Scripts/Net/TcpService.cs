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

        private Client mService;
        private IPAddress mIP;
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

        public bool IsConnected { get { return Client!=null && Connected; } }

        private int mConnectTimes = 0;
        public new bool Connect(string ip, int port)
        {
            if (IsConnected)
            {
                return true;
            }

            mIP = IPAddress.Parse(ip);
            mPort = port;
           

            BeginConnect(mIP, mPort, ConnectResult, this);

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

        void ConnectResult(IAsyncResult result)
        {
            try
            {
                mConnectTimes += 1;

                if (result.IsCompleted == false)
                {
                    if(mConnectTimes < 5)
                    {
                        BeginConnect(mIP, mPort, ConnectResult, this);
                    }
                    else
                    {
                        EndConnect(result);
                        Close();

                        return;
                    }
                }

                EndConnect(result);

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
                        continue;
                    }

                    if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE)
                    {
                        continue;
                    }
                  

                    if (MessageBuffer.IsValid(MessageBuffer.head) == false)
                    {
                        continue;
                    }
                    int bodySize = 0; 
                    if(MessageBuffer.Decode(MessageBuffer.head, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET, ref bodySize)==false)
                    {
                        continue;
                    }

                    MessageBuffer message = new MessageBuffer(MessageBuffer.MESSAGE_HEAD_SIZE + bodySize);
                   
                    Array.Copy(MessageBuffer.head, 0, message.buffer, 0, MessageBuffer.head.Length);

                    if (bodySize > 0)
                    {
                        int receiveBodySize = Client.Receive(message.buffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

                        if (receiveBodySize != bodySize)
                        {
                            continue;
                        }
                    }
                    if (onMessage != null)
                    {
                        onMessage(message);
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