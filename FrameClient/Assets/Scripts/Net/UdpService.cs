using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class UdpService
    {
        private Client mService;

        private Queue<MessageBuffer> mSendMessageQueue = new Queue<MessageBuffer>();

        private IPEndPoint  mUDPAdress;
        private UdpClient   mUDPClient;

        private Thread      mReceiveThread, mSendThread;


        public bool IsConnected { get { return mUDPClient!=null && mUDPClient.Client.Connected; } }

        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnet;
        public event OnExceptionHandler onException;


        public UdpService(Client service)
        {
            mService = service;
                  
        }

        public bool Connect(string ip, int port)
        {
            if(IsConnected)
            {
                return true;
            }

            mUDPAdress = new IPEndPoint(IPAddress.Parse(ip), port);
            mUDPClient = new UdpClient();

            mUDPClient.Connect(mUDPAdress);

            if(IsConnected ==false)
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
            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
            }
        }

        public void Close()
        {
            if(mUDPClient!=null)
            {
                mUDPClient.Close();
                mUDPClient = null;
            }
            if(mReceiveThread!=null)
            {
                mReceiveThread.Abort();
                mReceiveThread = null;
            }
            if(mSendThread!=null)
            {
                mSendThread.Abort();
                mSendThread = null;
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
                    lock (mSendMessageQueue)
                    {
                        while (mSendMessageQueue.Count > 0)
                        {
                            MessageBuffer message = mSendMessageQueue.Dequeue();
                            if (message == null) continue;

                            int ret = mUDPClient.Send(message.buffer, message.length);
                        }
                        mSendMessageQueue.Clear();
                    }

                }
                catch (Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(10);
            }
        }

        void ReceiveThread()
        {
            while (IsConnected)
            {
                try
                {
                    IPEndPoint ip = mUDPAdress;
                    byte[] data = mUDPClient.Receive(ref ip);

                    if (data.Length > 0 && MessageBuffer.IsValid(data))
                    {
                        if (onMessage != null)
                        {
                            onMessage(new MessageBuffer(data));
                        }
                    }
                }
                catch(Exception e)
                {
                    Close();
                    throw e;
                }

                Thread.Sleep(10);
            }
        }

    }
}
