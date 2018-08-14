using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    public class KcpService: UdpClient
    {
        private NetworkService mService;

        private int mPort;

        private Thread mReceiveThread, mUpdateThread;

        private bool mListening = false;

        public bool IsActive { get { return base.Client!=null&& base.Client.IsBound && mListening; } }
     
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
                        var sessions = mService.sessions;

                        for(int i = 0; i < sessions.Count; ++i)
                        {
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
                var sessions = mService.sessions;

                for (int i = 0; i < sessions.Count; ++i)
                {
                    sessions[i].UpdateKcp();
                }

                Thread.Sleep(1);
            }
        }

        
    }
}
