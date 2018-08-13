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
                  
                    if (c == null)
                    {
                        var sessions = mService.sessions;//一个临时的队列
                        for (int i = 0; i < sessions.Count; ++i)
                        {
                            if (sessions[i] == null)
                            {
                                continue;
                            }
                            sessions[i].OnReceiveKcp(data, ip);
                        }
                    }
                    else
                    {                    
                        c.OnReceiveKcp(data, ip);
                    }

                    Thread.Sleep(1);

                }
                catch (SocketException e)
                {
                    //mService.Debug(e.Message);
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
