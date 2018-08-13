using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public class KcpService: UdpClient
    {
        private NetworkService mService;

        private int mPort;

        private Thread mReceiveThread, mUpdateThread;

        private bool mListening = false;

        public bool IsActive { get { return Client.IsBound && mListening; } }

        public event OnUdpConnectHandler onConnect;

     
        public KcpService(NetworkService service, int port):base(port)
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
            mUpdateThread = new Thread(UpdateThread);


            mReceiveThread.Start();
            mUpdateThread.Start();

            return true;
        }

        public new void Close()
        {
            base.Close();

            if (mUpdateThread != null)
            {
                mUpdateThread.Abort();
                mUpdateThread = null;
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

                    Session c = mService.GetSession(ip);

                    if (data.Length == 4 || data.Length == 28)
                    {
                        int id = BitConverter.ToInt32(data, 0);

                        c = mService.GetSession(id);

                        if (c != null && (c.udpAdress == null || c.udpAdress.Equals(id) == false))
                        {
                            c.udpAdress = ip;
                            if (onConnect != null)
                            {
                                onConnect(c);
                            }
                        }
                    }

                    if (c == null)
                    {
                        var buffer = new MessageBuffer(data);
                        if (buffer.IsValid())
                        {
                            if (c == null || c.id != buffer.extra())
                            {
                                c = mService.GetSession(buffer.extra());
                            }
                        }
                    }

                    if (c != null)
                    {
                        c.OnReceiveKcp(data);
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



        void UpdateThread()
        {
            while (IsActive)
            {
                var sessions = mService.sessions;//一个临时的队列
                for (int i = 0; i < sessions.Count; ++i)
                {
                    if (sessions[i] == null)
                    {
                        continue;
                    }
                    sessions[i].UpdateKcp();
                }

                Thread.Sleep(1);
            }
        }
    }
}
