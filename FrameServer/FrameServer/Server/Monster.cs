using System;
using PBMessage;


namespace FrameServer
{
    public  class Monster
    {
        private int mRoleId;
      
        private Point3D mPosition = new Point3D();
        private Point3D mDirection = new Point3D();

        public int roleid { get { return mRoleId; } }
      
        public Point3D position { get { return mPosition; } set { mPosition = value; } }
        public Point3D direction { get { return mDirection; } set { mDirection = value; } }

        public PlayerInfo mPlayerInfo = new PlayerInfo();

        public Monster(int roleId)
        {
            mRoleId = roleId;

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
            mPlayerInfo.type = 1;//怪物
        }
    }
}
