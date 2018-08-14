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

        public bool IsActive { get { return base.Client!=null&& base.Client.IsBound && mListening; } }

        public event OnReceiveHandler onReceive;


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
                        Session c = mService.GetSession(ip);

                        var buffer = new MessageBuffer(data);

                        if (buffer.IsValid())
                        {
                            if (c == null || c.id != buffer.extra())
                            {
                                c = mService.GetSession(buffer.extra());
                            }

                            if (c != null)
                            {
                                if (c.udpAdress == null || c.udpAdress.Equals(ip) == false)
                                {
                                    c.udpAdress = ip;
                                }

                                if (onReceive != null)
                                {
                                    onReceive(new MessageInfo(buffer, c));
                                }
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
