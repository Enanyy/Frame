using System;
using Network;
using PBMessage;

namespace FrameServer
{

    public class User
    {
        private int mRoleId;
        private Session mClient;
        private bool mReady;

        private Point3D mPosition;
        private Point3D mDirection;

        public int roleid { get { return mRoleId; } }
        public Session client { get { return mClient; } }
        public bool ready { get { return mReady; } }
        public Point3D position { get { return mPosition; }set { mPosition = value; } }
        public Point3D direction { get { return mDirection; } set { mDirection = value; } }

        public PlayerInfo mPlayerInfo = new PlayerInfo();

        public User(int roleId, Session c)
        {
            mRoleId = roleId;
            mClient = c;
            mReady = false;

            mPlayerInfo.roleid = roleid;
            mPlayerInfo.name = mRoleId.ToString();
            mPlayerInfo.moveSpeed = 500;
            mPlayerInfo.moveSpeedAddition = 0;
            mPlayerInfo.moveSpeedPercent = 0;
            mPlayerInfo.attackSpeed = 100;
            mPlayerInfo.attackSpeedAddition = 0;
            mPlayerInfo.attackSpeedPercent = 0;
            mPlayerInfo.maxBlood = 100;
            mPlayerInfo.nowBlood = 100;
            mPlayerInfo.type = 0; //人物
        }


   

        public void SetReady()
        {
            mReady = true;
        }

        public void SendUdp<T>(MessageID messageId, T t) where T : class, ProtoBuf.IExtensible
        {
            if (mClient != null)
            {
                byte[] data = ProtoTransfer.SerializeProtoBuf<T>(t);
                MessageBuffer message = new MessageBuffer((int)messageId, data, mClient.id);
                mClient.SendUdp(message);
            }
        }

       

        public void SendTcp<T>(MessageID messageId, T t) where T : class, ProtoBuf.IExtensible
        {
            if (mClient != null)
            {
                byte[] data = ProtoTransfer.SerializeProtoBuf<T>(t);
                MessageBuffer message = new MessageBuffer((int)messageId, data, mClient.id);
                mClient.SendTcp(message);
            }
        }

       

        public void Disconnect()
        {
            mClient.Disconnect();
        }
    }
}
