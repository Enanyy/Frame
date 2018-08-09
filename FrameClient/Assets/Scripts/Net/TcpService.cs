using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class TcpService
    {
        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();
        private IPEndPoint mTCPAdress;
        private TcpClient mTCPSocket;

        private Client mService;
        private int mTCPPort;

        private int mSock;

        private Thread mReceiveThread, mSendThread, mActiveThread;

        public event OnAcceptPollHandler onAcceptPoll;
        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnect;
        public event OnExceptionHandler onException;

        public TcpService(Client service)
        {
            mService = service;
        }

        public bool IsConnected { get { return mTCPSocket != null && mTCPSocket.Connected;  } }

        public bool Connect(string ip, int port)
        {
            if(IsConnected)
            {
                return true;
            }
            mTCPSocket = new TcpClient();

            mTCPAdress = new IPEndPoint(IPAddress.Parse(ip), port);

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
                mTCPSocket.Connect(mTCPAdress);

                if(IsConnected == false)
                {
                    Close();
                    return;
                }

                while (true)
                {
                    //Read accepted ID
                    NetworkStream s = mTCPSocket.GetStream();

                    if (s.CanRead)
                    {
                        byte[] buff = new byte[4];

                        for (int i = 0; i < buff.Length; i++)
                            buff[i] = (byte)s.ReadByte();

                        //Send it back
                        mSock = BitConverter.ToInt32(buff, 0);

                        break;
                    }
                }

                mReceiveThread = new Thread(ReceiveThread);
                mSendThread = new Thread(SendThread);
                mActiveThread = new Thread(ActiveThread);


                mReceiveThread.Start();
                mSendThread.Start();
                mActiveThread.Start();

                if (onAcceptPoll != null)
                {
                    onAcceptPoll(mSock);
                }
                else if(onConnect!=null)
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

        public void Close()
        {
            if (mTCPSocket != null )
            {
                if (IsConnected)
                {
                    mTCPSocket.GetStream().Close();
                }
                mTCPSocket.Close();
                mTCPSocket = null;
            }

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

                            mTCPSocket.GetStream().Write(message.buffer, 0, message.length);
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
                    byte[] headbuffer = new byte[MessageBuffer.MESSAGE_HEAD_SIZE];
                    int receiveSize = mTCPSocket.Client.Receive(headbuffer, MessageBuffer.MESSAGE_HEAD_SIZE, SocketFlags.None);
                    if (receiveSize == 0)
                    {
                        return;
                    }

                    if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE)
                    {
                        return;
                    }
                    int messageId = BitConverter.ToInt32(headbuffer, MessageBuffer.MESSAGE_ID_OFFSET);
                    int bodySize = BitConverter.ToInt32(headbuffer, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET);

                    if (MessageBuffer.IsValid(headbuffer) == false)
                    {
                        return;
                    }

                    byte[] messageBuffer = new byte[MessageBuffer.MESSAGE_HEAD_SIZE + bodySize];
                    Array.Copy(headbuffer, 0, messageBuffer, 0, headbuffer.Length);

                    if (bodySize > 0)
                    {
                        int receiveBodySize = mTCPSocket.Client.Receive(messageBuffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

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