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


        public bool IsConnected { get { return Client.Connected; } }

        public event OnConnectHandler onConnect;
        public event OnMessageHandler onMessage;
        public event OnDisconnectHandler onDisconnet;
        public event OnExceptionHandler onException;


        public UdpService(Client service)
        {
            mService = service;

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
            lock (mSendMessageQueue)
            {
                mSendMessageQueue.Enqueue(message);
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

                            int ret = Send(message.buffer, message.length);
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
                    IPEndPoint ip = mServerAdress;
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0 && MessageBuffer.IsValid(data))
                    {
                        if (onMessage != null)
                        {
                            onMessage(new MessageBuffer(data));
                        }
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
    }
}
