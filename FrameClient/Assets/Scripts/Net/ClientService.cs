using System;
using PBMessage;
using System.Collections.Generic;

namespace Network
{
    public enum ClientID
    {
        Frame,
    }

 
    public class ClientService:Singleton<ClientService>
    {
        private Dictionary<int, Client> mClientDic = new Dictionary<int, Client>();

        public event OnDebugHandler onDebug;

        public void Connect(ClientID varClientID, string ip, int tcpPort, int udpPort, OnConnectHandler onConnect, OnDisconnectHandler onDisconnect)
        {
            int id = (int)varClientID;

            if(mClientDic.ContainsKey(id))
            {
                Client client = mClientDic[id];
                mClientDic.Remove(id);
                client.Disconnect();
            }

            Client c = new Client();

            c.onConnect += onConnect;
            c.onDisconnect += onDisconnect;
            c.onDebug += onDebug;

            c.onMessage += OnMessage;

            c.Connect(ip, tcpPort, udpPort);

            mClientDic.Add(id, c);
        }

        public Client GetClient(ClientID clientID)
        {
            int id = (int)clientID;
            if(mClientDic.ContainsKey(id))
            {
                return mClientDic[id];
            }
            return null;
        }
       
        
        public void Update()
        {
            var it = mClientDic.GetEnumerator();
            while(it.MoveNext())
            { 
                if (it.Current.Value.IsConnected)
                {
                    it.Current.Value.Update();
                }
            }
            it.Dispose();
        }    

       
        /// <summary>
        /// 通过UDP发送
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageId"></param>
        /// <param name="data"></param>
        public void SendUdp<T>(ClientID clientID, MessageID id, T data) where T : class, ProtoBuf.IExtensible
        {
            var client = GetClient(clientID);

            if(client== null)
            {
                return;
            }

            byte[] bytes = ProtoTransfer.SerializeProtoBuf<T>(data);

            MessageBuffer buffer = new MessageBuffer((int)id, bytes, client.acceptSock);

            client.SendUdp(buffer);
        }

        /// <summary>
        /// 通过TCP协议发送
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageId"></param>
        /// <param name="data"></param>
        public void SendTcp<T>(ClientID clientID, MessageID id, T data) where T : class, ProtoBuf.IExtensible
        {
            var client = GetClient(clientID);

            if (client == null)
            {
                return;
            }

            byte[] bytes = ProtoTransfer.SerializeProtoBuf<T>(data);

            MessageBuffer buffer = new MessageBuffer((int)id, bytes,  client.acceptSock);

            client.SendTcp(buffer);
        }



        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect(ClientID clientID)
        {
            if(mClientDic.ContainsKey((int)clientID))
            {
                mClientDic.Remove((int)clientID);
                mClientDic[(int)clientID].Disconnect();
            }
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        public void Disconnect()
        {
            foreach(var  v in mClientDic.Values)
            {
                v.Disconnect();
            }
            mClientDic.Clear();
        }

        private void OnMessage(MessageBuffer msg)
        {
            if(msg == null)
            {
                return;
            }
                  
            MessageDispatch.Dispatch(msg);
        }
    }
}
