using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    public delegate void OnConnectHandler();
    public delegate void OnMessageHandler(MessageBuffer msg);
    public delegate void OnDisconnectHandler();
    public delegate void OnExceptionHandler(Exception e);
    public delegate void OnPingHandler(int m);
    public delegate void OnDebugHandler(string msg);
    public delegate void OnAcceptPollHandler(int sock);


    public class Client
    {     
        static readonly byte pingByte = byte.MaxValue;

        List<string> mDebugMessageList = new List<string>();
       

        Queue<MessageBuffer> mReceiveMessageQueue = new Queue<MessageBuffer>();
        Queue<bool> mConnectResultQueue = new Queue<bool>();

        public event OnConnectHandler onConnect;
        public event OnDisconnectHandler onDisconnect;
        public event OnMessageHandler onMessage;
        public event OnExceptionHandler onException;
        public event OnPingHandler onPing;
        public event OnDebugHandler onDebug;

        
        public bool IsConnected
        {
            get
            {
                if(mTcp!=null)
                {
                    if(mUdp!=null)
                    {
                        return mTcp.IsConnected && mUdp.IsConnected;
                    }
                    else
                    {
                        return mTcp.IsConnected;
                    }
                }
                return false;
            }
        }

        private TcpService mTcp;
        private UdpService mUdp;
        private KcpService mKcp;

        private string mIP;
        private int mTCPPort;
        private int mUDPPort;

        private int mAcceptSock = 0;
        public int acceptSock { get { return mAcceptSock; } }

      
        public Client()
        {
        }

        public void Connect(string ip, int tcpPort, int udpPort, bool kcp)
        {
            if (IsConnected) return;

            mTcp = new TcpService(this);

            mIP = ip;
            mTCPPort = tcpPort;
            mUDPPort = udpPort;

            if (mUDPPort != 0)
            {
                mTcp.onAcceptPoll += OnAcceptPoll;
            }
            else
            {
                mTcp.onConnect += OnConnect;
            }
            mTcp.onDisconnect += OnDisconnect;
            mTcp.onException += onException;
            mTcp.onMessage += OnReceive;
      
            mTcp.Connect(mIP, mTCPPort);

            if(kcp == false)
            {
                mUdp = new UdpService(this);
            }
          
        }

        void OnAcceptPoll(int sock)
        {
            mAcceptSock = sock;

            if (mUdp != null)
            {
                mUdp.onConnect += OnConnect;
                mUdp.onDisconnet += OnDisconnect;
                mUdp.onException += onException;
                mUdp.onMessage += OnReceive;

                mUdp.Connect(mIP, mUDPPort);
            }
            else
            {
                mKcp = new KcpService(this, (uint)mAcceptSock);
                mKcp.onConnect += OnConnect;
                mKcp.onDisconnet += OnDisconnect;
                mKcp.onException += onException;
                mKcp.onMessage += OnReceive;

                mKcp.Connect(mIP, mUDPPort);
            }

            byte[] buffer = BitConverter.GetBytes(sock);
            SendUdp(new MessageBuffer(buffer));
        }

 
        public void Debug(string s)
        {
            mDebugMessageList.Add(s);
        }

        /// <summary>
        /// 在主线程中再调用回调
        /// </summary>
        private void OnConnect()
        {
            lock(mConnectResultQueue)
            {
                mConnectResultQueue.Enqueue(true);
            }
        }
        /// <summary>
        /// 在主线程中再调用回调
        /// </summary>
        private void OnDisconnect()
        {
            lock (mConnectResultQueue)
            {
                mConnectResultQueue.Enqueue(false);
            }
        }

        /// <summary>
        /// 主线程
        /// </summary>
        public void Update()
        {
            lock (mReceiveMessageQueue)
            {
                while (mReceiveMessageQueue.Count > 0)
                {
                    MessageBuffer message = mReceiveMessageQueue.Dequeue();
                    if (message == null) continue;

                    onMessage(message);
                }
            }

            lock(mConnectResultQueue)
            {
                while(mConnectResultQueue.Count > 0)
                {
                    bool result = mConnectResultQueue.Dequeue();
                    if(result)
                    {
                        if (onConnect != null) onConnect();
                    }
                    else
                    {
                        if (onDisconnect != null) onDisconnect();
                    }
                }
            }

            lock (mDebugMessageList)
            {
                for (int i = 0; i < mDebugMessageList.Count; ++i)
                {
                    if (onDebug != null) onDebug(mDebugMessageList[i]);
                }

                mDebugMessageList.Clear();
            }

           
        }

        void OnReceive(MessageBuffer message)
        {
            if (message == null)
            {
                return;
            }
            lock (mReceiveMessageQueue)
            {
                mReceiveMessageQueue.Enqueue(message);
            }
        }
  

        public void Disconnect()
        {
            if (mTcp != null)
            {
                mTcp.Close();
            }
            if (mUdp != null)
            {
                mUdp.Close();
            }
            if(mKcp!=null)
            {
                mKcp.Close();
            }
        }

        public void SendUdp(MessageBuffer msg)
        {
            if(msg == null)
            {
                return;
            }
            if (mKcp != null)
            {
                mKcp.Send(msg);
            }
            else if (mUdp != null)
            {
                mUdp.Send(msg);
            }
        }

        public void SendTcp(MessageBuffer msg)
        {
            if (msg == null || mTcp == null)
            {
                return;
            }
            mTcp.Send(msg);
        }

       
       

        public static long Ping(IPEndPoint ip)
        {
            UdpClient client = new UdpClient();

            client.Connect(ip);

            Stopwatch watch = Stopwatch.StartNew();

            client.Send(new byte[] { pingByte }, 1);
            var data = client.Receive(ref ip);

            long millis = watch.Elapsed.Milliseconds;
            watch.Stop();

            client.Close();

            return millis;
        }
    }
}

