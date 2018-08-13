using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class UdpService : UdpClient
    {
        private Client mService;

        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();

        private IPEndPoint mServerAdress;

        private Thread mReceiveThread, mSendThread;


        public bool IsConnected { get { return Client !=null && Client.Connected; } }

        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnet;
        public event OnExceptionHandler onException;

        public bool IsKcp { get { return mKCP!=null; } }

        private KCP mKCP;
        private uint mNextUpdateTime = 0;
        private static readonly DateTime utc_time = new DateTime(1970, 1, 1);

        private static uint current
        {
            get
            {
                return (uint)(Convert.ToInt64(DateTime.UtcNow.Subtract(utc_time).TotalMilliseconds) & 0xffffffff);
            }
        }
        public UdpService(Client service, int sock, bool kcp)
        {
            mService = service;

            if(kcp)
            {
                mKCP = new KCP((uint)sock, OnSendKcp);
                mKCP.NoDelay(1, 10, 2, 1);
            }
        }

        public new bool Connect(string ip, int port)
        {
            if (IsConnected)
            {
                return true;
            }

            mServerAdress = new IPEndPoint(IPAddress.Parse(ip), port);


            Connect(mServerAdress);

            if (IsConnected == false)
            {
                Close();
                return false;
            }

            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);
            mReceiveThread.Start();
            mSendThread.Start();


            if (onConnect != null)
            {
                onConnect();
            }
            return true;
        }

        public void Send(MessageBuffer message)
        {
            if (message == null)
            {
                return;
            }
            if (IsKcp == false)
            {
                lock (mSendMessageQueue)
                {
                    mSendMessageQueue.Enqueue(message);
                }
            }
            else
            {
                mKCP.Send(message.buffer);
                mNextUpdateTime = 0;
            }
        }

        public new void Close()
        {
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

            mKCP = null;

            if (onDisconnet != null)
            {
                onDisconnet();
                onDisconnet = null;
            }
        }

        void SendThread()
        {
            while (IsConnected)
            {
                try
                {
                    if (IsKcp == false)
                    {
                        lock (mSendMessageQueue)
                        {
                            while (mSendMessageQueue.Count > 0)
                            {
                                MessageBuffer message = mSendMessageQueue.Dequeue();
                                if (message == null) continue;

                                int ret = Send(message.buffer, message.length);
                            }
                            mSendMessageQueue.Clear();
                        }
                    }
                    else
                    {
                       UpdateKcp();  
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

        void ReceiveThread()
        {
            while (IsConnected)
            {
                try
                {
                    IPEndPoint ip = mServerAdress;
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0)
                    {
                        if (IsKcp == false)
                        {
                            if (onMessage != null && MessageBuffer.IsValid(data))
                            {
                                onMessage(new MessageBuffer(data));
                            }
                        }
                        else
                        {
                            OnReceiveKcp(data);
                        }
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

        #region KCP
        private void OnSendKcp(byte[] data, int length)
        {
            if (IsConnected && IsKcp)
            {
                int ret = Send(data, length);
                mService.Debug(ret.ToString());
            }
        }

        private void UpdateKcp()
        {
            if (mKCP != null)
            {
                uint time = current;
                if (time >= mNextUpdateTime)
                {
                    mKCP.Update(time);
                    mNextUpdateTime = mKCP.Check(time);
                }

            }
        }

        private void OnReceiveKcp(byte[] data)
        {
            if (mKCP != null)
            {
                mKCP.Input(data);

                for(int size = mKCP.PeekSize(); size > 0; size = mKCP.PeekSize())
                {
                    byte[] buffer = new byte[size];
                    if (mKCP.Recv(buffer) > 0)
                    {
                        MessageBuffer message = new MessageBuffer(buffer);
                        if (onMessage != null && message.IsValid())
                        {
                            onMessage(message);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
