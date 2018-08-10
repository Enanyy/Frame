using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Network
{
    public class UdpService : UdpClient
    {
        private NetworkService mService;

        private int mPort;
        private Queue<MessageInfo> mSendMessageQueue = new Queue<MessageInfo>();

        private Thread mReceiveThread, mSendThread;

        private bool mListening = false;

        public bool IsActive { get { return Client.IsBound && mListening; } }

        public event OnReceiveHandler onReceive;
        public event OnUdpConnectHandler onConnect;


        public UdpService(NetworkService service, int port) : base(port)
        {
            mService = service;
            mPort = port;
        }

        public bool Listen()
        {
            if (mListening)
            {
                return true;
            }

            mListening = true;

            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);


            mReceiveThread.Start();
            mSendThread.Start();

            return true;
        }

        public void Send(MessageInfo message)
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

            if (mSendThread != null)
            {
                mSendThread.Abort();
                mSendThread = null;
            }
            if (mReceiveThread != null)
            {
                mReceiveThread.Abort();

                mReceiveThread = null;
            }
        }

        void ReceiveThread()
        {
            while (IsActive)
            {
                try
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = Receive(ref ip);

                    if (data.Length > 0)
                    {
                        var buffer = new MessageBuffer(data);

                        Session c = mService.GetSession(ip);

                        //Pinged
                        if (data.Length == 1 && data[0] == NetworkService.pingByte)
                        {
                            if (c != null && c.Pinging)
                            {
                                c.Ping();
                            }
                            else
                            {
                                Send(data, 1, ip);
                            }
                        }

                        else if (data.Length == 4)
                        {
                            int id = BitConverter.ToInt32(data, 0);
                            c = mService.GetSession(id);

                            if (c != null)
                            {
                                c.udpAdress = ip;
                                if (onConnect != null)
                                {
                                    onConnect(c);
                                }
                            }
                        }
                        else if (buffer.IsValid())
                        {
                            if (c == null || c.id != buffer.extra())
                            {
                                c = mService.GetSession(buffer.extra());
                            }

                            if (onReceive != null && c != null)
                            {
                                onReceive(new MessageInfo(buffer, c));
                            }
                        }
                    }

                    Thread.Sleep(1);

                }
                catch (SocketException e)
                {
                    mService.Debug(e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    mService.CatchException(e);
                    throw e;

                }

            }
        }



        void SendThread()
        {
            while (IsActive)
            {
                lock (mSendMessageQueue)
                {
                    while (mSendMessageQueue.Count > 0)
                    {
                        MessageInfo message = mSendMessageQueue.Dequeue();

                        if (message == null) continue;

                        try
                        {
                           Send(message.buffer.buffer, message.buffer.size, message.session.udpAdress);
                        }
                        catch (SocketException e)
                        {
                            mService.Debug(e.Message);
                        }
                        catch (Exception e)
                        {
                            mService.CatchException(e);
                            throw e;
                        }
                    }
                    mSendMessageQueue.Clear();
                }

                Thread.Sleep(1);
            }
        }
    }
}
