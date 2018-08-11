using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Network
{
    public class Session
    {
        public int id;
        public IPEndPoint tcpAdress, udpAdress;

        private NetworkService mService;
        private Socket mSocket;
        private Stopwatch mPingWatch;
        private Thread mActiveThread;

        public Socket socket { get { return mSocket; } }
        public NetworkService service { get { return mService; } }


        public bool Pinging
        {
            get
            {
                return mPingWatch != null;
            }
        }

        public Session(int clientId, Socket sock, NetworkService serv)
        {
            id = clientId;
            mService = serv;
            mSocket = sock;

            tcpAdress = (IPEndPoint)sock.RemoteEndPoint;
            mActiveThread = new Thread(ActiveThread);

            mActiveThread.Start();

        }

        public void SendAcceptPoll()
        {
            mSocket.Send(BitConverter.GetBytes(id));
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
            if(mService!=null)
            {
                mService.SendUdp(message, this);
            }
        }

       
        public void SendTcp(MessageBuffer message) 
        {
            if(mService!=null)
            {
                mService.SendTcp(message, this);
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

        public void Ping()
        {
            if (Pinging)
            {
                mService.PingResult(this, mPingWatch.Elapsed.Milliseconds);
                mPingWatch = null;
            }
            else
            {
                mPingWatch = Stopwatch.StartNew();
                mService.SendUdp(new MessageBuffer(new byte[] { NetworkService.pingByte }), this);
            }
        }

    }
}
