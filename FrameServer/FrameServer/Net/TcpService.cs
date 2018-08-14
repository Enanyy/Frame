using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Network
{
    public class TcpService : TcpListener
    {
        private NetworkService mService;

        private int mPort;

        Thread mAcceptThread, mReceiveThread, mSendThread;

        Queue<MessageInfo> mSendMessageQueue = new Queue<MessageInfo>();


        public bool IsActive { get { return Active; } }


        public event OnReceiveHandler onReceive;
        public event OnAcceptHandler onAccept;

        public TcpService(NetworkService service, int port) : base(IPAddress.Any, port)
        {
            mService = service;
            mPort = port;
        }

        public bool Listen()
        {
            if (IsActive)
            {
                return true;
            }

            Start();

            mAcceptThread = new Thread(AcceptThread);
            mReceiveThread = new Thread(ReceiveThread);
            mSendThread = new Thread(SendThread);

            mAcceptThread.Start();
            mReceiveThread.Start();
            mSendThread.Start();


            return true;
        }

        public void  Close()
        {
            Stop();

            if (mAcceptThread != null)
            {
                mAcceptThread.Abort();
                mAcceptThread = null;
            }

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

        void AcceptThread()
        {
            while (IsActive)
            {
                try
                {
                    Socket s = AcceptSocket();
                    if (s != null)
                    {
                        if (onAccept != null)
                        {
                            onAccept(s);
                        }
                    }

                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    mService.CatchException(e);
                    throw e;
                }

            }
        }

        void ReceiveThread()
        {
            while (IsActive)
            {
                var sessions = mService.sessions;
                for(int i = 0; i < sessions.Count; ++i)
                {
                    Session c = sessions[i];
                    if(c == null)
                    {
                        continue;
                    }
                    try
                    {
                        if (c.IsConnected == false)
                        {
                            continue;
                        }

                        int receiveSize = c.socket.Receive(MessageBuffer.head, MessageBuffer.MESSAGE_HEAD_SIZE, SocketFlags.None);
                        if (receiveSize == 0)
                        {
                            continue;
                        }

                        if (receiveSize != MessageBuffer.MESSAGE_HEAD_SIZE)
                        {
                            continue;
                        }
                       
                        if (MessageBuffer.IsValid(MessageBuffer.head) == false)
                        {
                            continue;
                        }
                        int bodySize = 0;
                        if (MessageBuffer.Decode(MessageBuffer.head, MessageBuffer.MESSAGE_BODY_SIZE_OFFSET, ref bodySize) == false)
                        {
                            continue;
                        }
                        MessageBuffer message = new MessageBuffer(MessageBuffer.MESSAGE_HEAD_SIZE + bodySize);

                        Array.Copy(MessageBuffer.head, 0, message.buffer, 0, MessageBuffer.head.Length);

                        if (bodySize > 0)
                        {
                            int receiveBodySize = c.socket.Receive(message.buffer, MessageBuffer.MESSAGE_BODY_OFFSET, bodySize, SocketFlags.None);

                            if (receiveBodySize != bodySize)
                            {
                                continue;
                            }
                        }

                        if (onReceive != null)
                        {
                            onReceive(new MessageInfo(message, c));
                        }

                    }
                    catch (SocketException e)
                    {
                        mService.Debug(e.Message);
                        c.Disconnect();
                    }
                    catch (Exception e)
                    {
                        mService.CatchException(e);
                        throw e;
                    }
                }

                Thread.Sleep(1);
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
                            message.session.socket.Send(message.buffer.buffer);
                        }
                        catch (SocketException e)
                        {
                            mService.Debug(e.Message);
                            message.session.Disconnect();
                        }
                        catch (Exception e)
                        {
                            mService.CatchException(e);
                            throw e;
                        }
                    }
                }
                Thread.Sleep(1);

            }
        }

      
    }
}
