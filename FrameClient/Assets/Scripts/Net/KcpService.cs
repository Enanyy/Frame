using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class KcpService: UdpClient
    {
        private Client mService;

        private IPEndPoint mServerAdress;

        private Thread mReceiveThread, mUpdateThread;


        public bool IsConnected { get { return Client != null && Client.Connected; } }

        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnet;
        public event OnExceptionHandler onException;

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

        public KcpService(Client service, uint conv)
        {
            mService = service;
            mKCP = new KCP(conv, OnSendKcp);
            mKCP.NoDelay(1, 10, 2, 1);

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
            mUpdateThread = new Thread(SendThread);
            mReceiveThread.Start();
            mUpdateThread.Start();


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
            if(mKCP == null)
            {
                return;
            }
            lock (mKCP)
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
            if (mUpdateThread != null)
            {
                mUpdateThread.Abort();
                mUpdateThread = null;

            }

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
                    UpdateKcp();

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
                        OnReceiveKcp(data);

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
            if (IsConnected)
            {
                int ret = Send(data, length);
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
            if (mKCP == null)
            {
                return;
            }
            lock (mKCP)
            {
                mKCP.Input(data);

                for (int size = mKCP.PeekSize(); size > 0; size = mKCP.PeekSize())
                {
                    MessageBuffer message = new MessageBuffer(size);
                    if (mKCP.Recv(message.buffer) > 0)
                    {
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
