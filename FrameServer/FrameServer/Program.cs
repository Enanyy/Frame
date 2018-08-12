using System;
using System.Collections.Generic;
using System.Threading;
using PBMessage;
using Network;

namespace FrameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            int deltaTime = 1;
            while (true)
            {
                Thread.Sleep(deltaTime);

                p.Tick(deltaTime);

            }
        }

        public const int TCP_PORT = 1255;
        public const int UDP_PORT = 1337;

        public const int FRAME_INTERVAL = 100; //帧时间 毫秒

        NetworkService mService;

        private int mRoleId = 100; //客户端的人物开始id
        private int mMonsterId = 100000; //客户端怪物开始id
        private const int SERVER_ROLEID = 0; //服务器也参与整局游戏，负责发送一些全局命令，比如Buff、怪物生成

        List<User> mUserList = new List<User>();
        List<Monster> mMonsterList = new List<Monster>();

        Dictionary<long, Dictionary<int, List<Command>>> mFrameDic = new Dictionary<long, Dictionary<int, List<Command>>>();//关键帧

        private bool mBegin = false;    //游戏是否开始

        private long mCurrentFrame = 1; //当前帧数
        private long mFrameTime = 0;
      

        public enum Mode
        {
            LockStep,
            Optimistic,
        }

        private Mode mMode = Mode.LockStep;

        public Program()
        {
            MessageBuffer.MESSAGE_MAX_VALUE = (int)MessageID.MaxValue;
            MessageBuffer.MESSAGE_MIN_VALUE = (int)MessageID.MinValue;

            mService = new NetworkService(TCP_PORT, UDP_PORT);

            Debug.ENABLE_ERROR = true;

            mService.onStart += OnStart;
            mService.onConnect += OnConnect;
            mService.onMessage += OnMessage;
            mService.onDisconnect += OnDisconnect;
            mService.onDebug += OnDebug;


            Debug.Log("1. lockstep mode.");
            Debug.Log("2. optimistic mode.");
            
            string input = Console.ReadLine();

            mMode = input == "1" ? Mode.LockStep : Mode.Optimistic;

            mService.Start();
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msaageId"></param>
        /// <param name="data"></param>
        /// <param name="ready">是否只发给已经准备好的人</param>
        void BroadCast<T>(MessageID msaageId, T data, bool ready = false) where T : class, ProtoBuf.IExtensible
        {
            for (int i = 0; i < mUserList.Count; ++i)
            {
                if (ready == false || (ready == true && mUserList[i].ready))
                {
                    mUserList[i].SendUdp(msaageId, data);
                }
            }
        }

        User GetUser(int roleid)
        {
            for(int i = 0; i < mUserList.Count; ++i)
            {
                if(mUserList[i].roleid == roleid)
                {
                    return mUserList[i];
                }
            }
            return null;
        }


        public void Tick(int deltaTime)
        {
            mService.Update();

            if(mBegin && mUserList.Count > 0)
            {
                mFrameTime += deltaTime;

                if(mMode == Mode.Optimistic)
                {
                    if(mFrameTime % FRAME_INTERVAL == 0)
                    {
                        SendFrame();
                    }
                }
            }
        }


        private void OnStart()
        {
            Debug.Log(string.Format("Server start success,mode={0} ip={1} tcp port={2} udp port={3}",mMode.ToString(), NetworkService.GetLocalIP(),TCP_PORT, UDP_PORT),ConsoleColor.Green);
           
        }

        private void OnConnect(Session c)
        {
            User user = new User(mRoleId++, c);
            mUserList.Add(user);

            Debug.Log(string.Format("{0} roleid={1} tcp={2}, udp={3} connected! count={4}", c.id, user.roleid, c.tcpAdress, c.udpAdress, mUserList.Count),ConsoleColor.Yellow);

            GM_Connect sendData = new GM_Connect();
            sendData.roleId = user.roleid;
            sendData.frameinterval = FRAME_INTERVAL; //告诉客户端帧时长 毫秒
            sendData.mode = (int)mMode;              //告诉客户端当前模式
            sendData.player = ProtoTransfer.Get(user.mPlayerInfo);

            user.SendUdp(MessageID.GM_CONNECT_SC, sendData);

            //告诉别人有人连接了
            for (int i = 0; i < mUserList.Count; ++i)
            {
                var u = mUserList[i]; //别人
                if (c != u.client)
                {
                    u.SendUdp(MessageID.GM_CONNECT_BC, sendData);
                }
            }

            //告诉他有哪些人已经连接了
            for (int i = 0; i < mUserList.Count; ++i)
            {
                var u = mUserList[i];//别人
                if (c != u.client)
                {
                    sendData.roleId = u.roleid;
                    sendData.player = ProtoTransfer.Get(u.mPlayerInfo);
                    //发给自己
                    user.SendUdp(MessageID.GM_CONNECT_BC, sendData);
                }
            }

        }

        private void OnMessage(Session client, MessageBuffer msg)
        {
            MessageID messageId = (MessageID)msg.id();
            switch (messageId)
            {
                case MessageID.GM_READY_CS:
                    {
                        GM_Ready recvData = ProtoTransfer.DeserializeProtoBuf<GM_Ready>(msg);
                        OnReceiveReady(client, recvData);

                    }
                    break;

                case MessageID.GM_FRAME_CS:
                    {
                        GM_Frame recvData = ProtoTransfer.DeserializeProtoBuf<GM_Frame>(msg);
                        if (mMode == Mode.LockStep)
                        {
                            OnLockStepFrame(client, recvData);
                        }
                        else
                        {
                            OnOptimisticFrame(client, recvData);
                        }
                    }
                    break;

            }
        }

      
        private void OnDisconnect(Session c)
        {
            if (c == null) return;

            int id = c.id;
           

            int roleId = 0;

            for (int i = 0; i < mUserList.Count; ++i)
            {
                if (mUserList[i].client == c)
                {
                    roleId = mUserList[i].roleid;
                    mUserList.RemoveAt(i);
                    break;
                }
            }
            Debug.Log(string.Format("{0} roleid={1}  disconnected!", id, roleId), ConsoleColor.Red);

            GM_Disconnect sendData = new GM_Disconnect();
            sendData.roleId = roleId;

            BroadCast(MessageID.GM_DISCONNECT_BC, sendData);
        }

        private void OnDebug(string s)
        {
            Debug.Log(s, ConsoleColor.Red);
        }

        private void OnReceiveReady(Session client, GM_Ready recvData)
        {
            if (recvData == null || client == null) return;
            int readyCount = 0;
            for (int i = 0; i < mUserList.Count; ++i)
            {
                var user = mUserList[i];
                if (recvData.roleId == user.roleid && client == user.client)
                {
                    user.position = ProtoTransfer.Get(recvData.position);
                    user.direction = ProtoTransfer.Get(recvData.direction);
                    user.SetReady();
                }
                //广播玩家准备（包括自己）
                user.SendUdp(MessageID.GM_READY_BC, recvData);

                if (user.ready)
                {
                    readyCount++;
                }
            }

            Debug.Log(string.Format("{0} roleid={1} ready, ready count={2} user count={3}", client.id, recvData.roleId, readyCount, mUserList.Count), ConsoleColor.Blue);

            if (mBegin == false)
            {
                //所有的玩家都准备好了，可以开始同步
                if (readyCount >= mUserList.Count)
                {
                    mFrameDic = new Dictionary<long, Dictionary<int, List<Command>>>();

                    GM_Begin sendData = new GM_Begin();
                    sendData.result = 0;

                    BroadCast(MessageID.GM_BEGIN_BC, sendData, true);

                    BeginGame();

                }
            }
            /*
            else //断线重连
            {           
                User user = GetUser(recvData.roleId);
                if(user!=null)
                {
                    GM_Begin sendData = new GM_Begin();
                    sendData.result = 0;

                    user.SendUdp(MessageID.GM_BEGIN_BC, sendData);

                    GM_Frame_BC frameData = new GM_Frame_BC();
                    //给他发送当前帧之前的数据
                    for (long frame = 1; frame < mCurrentFrame - 1; ++frame)
                    {
                        if (mFrameDic.ContainsKey(frame))
                        {
                            frameData.frame = frame;
                            frameData.frametime = 0;
                            var it = mFrameDic[frame].GetEnumerator();
                            while (it.MoveNext())
                            {
                                for (int i = 0, count = it.Current.Value.Count; i < count; ++i)
                                {
                                    GMCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);

                                    frameData.command.Add(cmd);
                                }
                            }
                            user.SendUdp(MessageID.GM_FRAME_BC, frameData);
                        }
                    }
                }
            }
            */
        }

        private void BeginGame()
        {
            mCurrentFrame = 1;

            mBegin = true; //游戏开始

            mFrameTime = 0;

            CreateMonster();
        }
        void CreateMonster()
        {
            //服务器添加命令

            for (int i = 0; i < 3; ++i)
            {

                Monster monster = new Monster(mMonsterId++);
                mMonsterList.Add(monster);

                monster.mPlayerInfo.name = "Server " + monster.roleid;
                monster.mPlayerInfo.type = 2;//Boss

                monster.position.x = ((i + 1) * (i % 2 == 0 ? -3 : 3)) * 10000;
                monster.position.y = 1 * 10000;
                monster.position.z = -10 * 10000;

                CMD_CreateMonster data = new CMD_CreateMonster();
                data.roleId = SERVER_ROLEID;
                data.player = ProtoTransfer.Get(monster.mPlayerInfo);
                data.position = ProtoTransfer.Get(monster.position);
                data.direction = ProtoTransfer.Get(monster.direction);

                Command cmd = new Command();
                cmd.Set(CommandID.CREATE_MONSTER, data);

                AddCommand(cmd);
            }

        }

        /// <summary>
        /// 服务器添加一个命令
        /// </summary>
        void AddCommand(Command cmd)
        {
            if (cmd == null)
            {
                return;
            }

            if (mFrameDic.ContainsKey(mCurrentFrame) == false)
            {
                mFrameDic[mCurrentFrame] = new Dictionary<int, List<Command>>();
            }

           
            cmd.SetFrame(mCurrentFrame, mFrameTime);

            if (mFrameDic[mCurrentFrame].ContainsKey(SERVER_ROLEID) == false)
            {
                mFrameDic[mCurrentFrame].Add(SERVER_ROLEID, new List<Command>());
            }
            mFrameDic[mCurrentFrame][SERVER_ROLEID].Add(cmd);

        }

        #region LockStep

        private void OnLockStepFrame(Session client, GM_Frame recvData)
        {
            long frame = recvData.frame;
            int roleId = recvData.roleId;

            if (recvData.command.Count > 0 || frame % 30 == 0)
            {
                Debug.Log(string.Format("Receive {0} serverframe:{1} clientframe:{2} command:{3}", roleId, mCurrentFrame, frame, recvData.command.Count), ConsoleColor.DarkGray);
            }
            if (mFrameDic.ContainsKey(frame) == false)
            {
                mFrameDic.Add(frame, new Dictionary<int, List<Command>>());
            }

            var frames = mFrameDic[frame];

            //当前帧的服务器命令
            if (frames.ContainsKey(SERVER_ROLEID)==false)
            {
                frames.Add(SERVER_ROLEID, new List<Command>());
            }

            //该玩家是否发送了当前帧
            if (frames.ContainsKey(roleId) == false)
            {
                frames.Add(roleId, new List<Command>());
            }

            for (int i = 0; i < recvData.command.Count; ++i)
            {
                Command cmd = new Command(recvData.command[i].frame, recvData.command[i].type, recvData.command[i].data, recvData.command[i].frametime);

                frames[roleId].Add(cmd);
            }

            //减去1是因为服务器命令也在当前帧中
            if (frames.Count - 1 == mUserList.Count)
            {
                GM_Frame_BC sendData = new GM_Frame_BC();
                sendData.frame = frame;
                sendData.frametime = mFrameTime;
                var it = frames.GetEnumerator();
                while(it.MoveNext())
                {
                    for(int i = 0,count = it.Current.Value.Count; i < count; ++i)
                    {
                        GMCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);
                      
                        sendData.command.Add(cmd);
                    }
                }


                BroadCast(MessageID.GM_FRAME_BC, sendData, true);

                mCurrentFrame = frame + 1;
            }
            else
            {
                Debug.Log(string.Format("Waiting {0} frame:{1} count:{2} current:{3} ", roleId, frame, mFrameDic[frame].Count, mUserList.Count),ConsoleColor.Red);
            }
        }

        #endregion

        #region 乐观模式
       
        /// <summary>
        /// 按固定频率向客户端广播帧
        /// </summary>
        private void SendFrame()
        {
            long frame = mCurrentFrame ++;
            int userCount = 0; //当前帧有多少个客户端发了命令
            GM_Frame_BC sendData = new GM_Frame_BC();

            sendData.frame = frame;
            sendData.frametime = mFrameTime;

            if (mFrameDic.ContainsKey(frame))
            {
                var frames = mFrameDic[frame];

                userCount = frames.Count;

                var it = frames.GetEnumerator();
                while (it.MoveNext())
                {
                    for (int i = 0, count = it.Current.Value.Count; i < count; ++i)
                    {
                        GMCommand cmd = ProtoTransfer.Get(it.Current.Value[i]);

                        sendData.command.Add(cmd);
                    }
                }
            }

           
            //不显示那么多log
            if (frame % 30 == 0 || sendData.command.Count > 0)
            {
                Debug.Log(string.Format("Send frame:{0} user count:{1} command count:{2}", frame, userCount, sendData.command.Count), ConsoleColor.Gray);
            }

            BroadCast(MessageID.GM_FRAME_BC, sendData,true);         
        }

        private void OnOptimisticFrame(Session client, GM_Frame recvData)
        {

            int roleId = recvData.roleId;

            long frame = recvData.frame;

            Debug.Log(string.Format("Receive roleid={0} serverframe:{1} clientframe:{2} command:{3}", roleId, mCurrentFrame, frame,recvData.command.Count),ConsoleColor.DarkYellow);
            
            if (mFrameDic.ContainsKey(mCurrentFrame) == false)
            {
                mFrameDic[mCurrentFrame] = new Dictionary<int, List<Command>>();
            }
            for (int i = 0; i < recvData.command.Count; ++i)
            {
                //乐观模式以服务器收到的时间为准
                Command frameData = new Command(recvData.command[i].frame, recvData.command[i].type, recvData.command[i].data, mFrameTime);
                if (mFrameDic[mCurrentFrame].ContainsKey(roleId) == false)
                {
                    mFrameDic[mCurrentFrame].Add(roleId, new List<Command>());
                }
                mFrameDic[mCurrentFrame][roleId].Add(frameData);
            }
        }

        #endregion


    }
}
