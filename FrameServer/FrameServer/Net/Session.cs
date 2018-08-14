using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Network
{
    public class Session
    {
        private int mID;
        public int id { get { return mID; } }
        public IPEndPoint tcpAdress, udpAdress;

        private NetworkService mService;
        private Socket mSocket;
        private Thread mActiveThread;

        public Socket socket { get { return mSocket; } }
        public NetworkService service { get { return mService; } }

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

  
        

        public Session(int id, Socket sock, NetworkService serv)
        {
            mID = id;
            mService = serv;
            mSocket = sock;

            tcpAdress = (IPEndPoint)sock.RemoteEndPoint;
            mActiveThread = new Thread(ActiveThread);

            mActiveThread.Start();

            if (mService.kcp!=null)
            {
                mKCP = new KCP((uint)id, OnSendKcp);
                mKCP.NoDelay(1, 10, 2, 1);
            }
        }

        void ActiveThread()
        {
            while (IsConnected)
            {
                Thread.Sleep(1000);
            }

            Disconnect();
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (mSocket != null && mSocket.Connected)
                    {
                        return true;
                    }
                    return false;

                }
                catch (SocketException e)
                {
                    mService.CatchException(e);
                    return false;
                }
                catch (Exception e)
                {
                    mService.CatchException(e);
                    return false;
                }
            }
        }

        public void SendUdp(MessageBuffer message) 
        {
            if (mService != null)
            {
                if (mService.kcp!=null)
                {
                    SendKcp(message);
                }
                else if(mService.udp!=null)
                {
                    mService.udp.Send(new MessageInfo( message, this));
                }
            }
        }

       
        public void SendTcp(MessageBuffer message) 
        {
            if(mService!=null && mService.tcp !=null)
            {
                mService.tcp.Send(new MessageInfo(message, this));
            }
        }

       
        public void Disconnect()
        {
            if (mSocket == null) return;

            mSocket.Close();
            mSocket = null;
            mService.RemoveSession(this);

            mActiveThread.Abort();
            mActiveThread = null;

        }

  



        #region KCP
        private void SendKcp(MessageBuffer message)
        {
            if (mKCP != null)
            {
                lock (mKCP)
                {
                    mKCP.Send(message.buffer);
                    mNextUpdateTime = 0;//可以马上更新
                }
            }
        }
        public void UpdateKcp()
        {
            if (mKCP == null)
            {
                return;
            }
            if (mService == null || mService.kcp == null || mService.kcp.IsActive == false)
            {
                return;
            }

            uint time = current;
            if (time >= mNextUpdateTime)
            {
                mKCP.Update(time);
                mNextUpdateTime = mKCP.Check(time);
            }
        }

        private void OnSendKcp(byte[] data, int length)
        {
            try
            {
                if (udpAdress!=null && mService != null && mService.kcp != null & mService.kcp.IsActive)
                {
                    mService.kcp.Send(data, length, udpAdress);
                }
            }
            catch (Exception e)
            {
                
            }
        }

        public void OnReceiveKcp(byte[] data, IPEndPoint ip)
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
                        if (message.IsValid() && message.extra() == id)
                        {
                            if (udpAdress == null || udpAdress.Equals(ip) == false)
                            {
                                udpAdress = ip;
                            }
                            mService.OnReceive(new MessageInfo(message, this));
                        }
                    }
                }
            }

        }
        #endregion
    }
}
