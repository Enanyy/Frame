using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Network;
using PBMessage;
using Util;
using System;
using System.Threading;

public class FrameScene : GameScene, IReceiverHandler
{
    public FrameScene() : base(GameSceneType.FrameScene)
    {
       
    }

    private int mFrameInterval = 100;//帧时长
    private long mCurrentFrame = 1; //当前帧
    private long mSentFrame = 0;   //已发送帧（LockStep）

    private bool mBegin = false;
   
    private long mFrameTime;   //当前帧的服务器时间
   
  

    private Queue<Command> mCommandQueue = new Queue<Command>();
    private Dictionary<long, List<Command>> mFrameDic = new Dictionary<long, List<Command>>();

    private Thread mTickThread;

    public override void OnEnter()
    {
        base.OnEnter();

        MessageBuffer.MESSAGE_MAX_VALUE = (int)MessageID.MaxValue;
        MessageBuffer.MESSAGE_MIN_VALUE = (int)MessageID.MinValue;

        ClientService.GetSingleton().onDebug += OnDebug;

        RegisterReceiver();

        WindowManager.GetSingleton().Open<UI_Main>();
      
    }

    public override void OnUpdate()
    {
        base.OnUpdate(); 
    }

    public override void OnExit()
    {
        base.OnExit();
        mCommandQueue.Clear();
        ClientService.GetSingleton().onDebug -= OnDebug;

        UnRegisterReceiver();  

        if(mTickThread!=null)
        {
            mTickThread.Abort();
            mTickThread = null;
        }
    }

    /// <summary>
    /// tick 帧时间
    /// </summary>
    private void Tick()
    {
        while (mBegin)
        {
            Thread.Sleep(1);

            mFrameTime += 1;

            if (GameApplication.GetSingleton().mode == Mode.LockStep)
            {
                if (mFrameTime % mFrameInterval == 0)
                {
                    SendFrame();
                }
            }
        }
    }

    private void OnConnectSuccess()
    {
        Debug.Log("Connect server success!");

        EventDispatch.Dispatch(EventID.Connect_Return, 0);
    }
    private void OnConnectFail()
    {
        Debug.Log("Connect server fail!");
        EventDispatch.Dispatch(EventID.Connect_Return, 1);
    }

    private void OnDebug(string message)
    {
        if(string.IsNullOrEmpty(message)==false)
        {
            Debug.Log(message);
        }
    }


    void CreatePlayerCharacter(PlayerInfo info)
    {
        
        if(info == null)
        {
            return;
        }

        PlayerManager.GetSingleton().CreatePlayerCharacter(info, (varCharacter) =>
        {
            if (varCharacter)
            {
                EventPlayerCreate.sData.Clear();
                EventPlayerCreate.sData.roleid = info.roleid;
                EventPlayerCreate.sData.name = info.name;
                EventPlayerCreate.sData.nowBlood = info.maxBlood;
                EventPlayerCreate.sData.maxBlood = info.nowBlood;
                EventPlayerCreate.sData.type = info.type;

                EventDispatch.Dispatch(EventID.Player_Create, EventPlayerCreate.sData);
            }
        });
    }

    #region Event

    public void RegisterReceiver()
    {
        #region Event
        EventDispatch.RegisterReceiver<int>(EventID.Ready_Request, OnReadyRequest);
        EventDispatch.RegisterReceiver<EventConnect>(EventID.Connect_Request, OnConnectRequest);
        EventDispatch.RegisterReceiver<Command>(EventID.AddCommand, OnAddCommand);
        #endregion
        #region Message
        MessageDispatch.RegisterReceiver<GM_Return>(MessageID.GM_ACCEPT_SC, OnAccept);
        MessageDispatch.RegisterReceiver<GM_Connect>(MessageID.GM_CONNECT_SC, OnConnectReturn);
        MessageDispatch.RegisterReceiver<GM_Connect>(MessageID.GM_CONNECT_BC, OnConnectBC);
        MessageDispatch.RegisterReceiver<GM_Disconnect>(MessageID.GM_DISCONNECT_BC, OnDisconnectBC);
        MessageDispatch.RegisterReceiver<GM_Ready>(MessageID.GM_READY_BC, OnReadyBC);
        MessageDispatch.RegisterReceiver<GM_Begin>(MessageID.GM_BEGIN_BC, OnBeginBC);
        MessageDispatch.RegisterReceiver<GM_Frame_BC>(MessageID.GM_FRAME_BC, OnFrameBC);

        #endregion

        #region Command
        CommandDispatch.RegisterReceiver<CMD_ReleaseSkill>(CommandID.RELEASE_SKILL, OnCommandReleaseSkill);
        CommandDispatch.RegisterReceiver<CMD_MoveToPoint>(CommandID.MOVE_TO_POINT, OnCommandMoveToPoint);
        CommandDispatch.RegisterReceiver<CMD_CreateMonster>(CommandID.CREATE_MONSTER, OnCreateMonster);
      
        #endregion
    }

    public void UnRegisterReceiver()
    {
        #region Event
        EventDispatch.UnRegisterReceiver<int>(EventID.Ready_Request, OnReadyRequest);
        EventDispatch.UnRegisterReceiver<EventConnect>(EventID.Connect_Request, OnConnectRequest);
        EventDispatch.UnRegisterReceiver<Command>(EventID.AddCommand, OnAddCommand);
        #endregion
        #region Message
        MessageDispatch.UnRegisterReceiver<GM_Return>(MessageID.GM_ACCEPT_SC, OnAccept);
        MessageDispatch.UnRegisterReceiver<GM_Connect>(MessageID.GM_CONNECT_SC, OnConnectReturn);
        MessageDispatch.UnRegisterReceiver<GM_Connect>(MessageID.GM_CONNECT_BC, OnConnectBC);
        MessageDispatch.UnRegisterReceiver<GM_Disconnect>(MessageID.GM_DISCONNECT_BC, OnDisconnectBC);
        MessageDispatch.UnRegisterReceiver<GM_Ready>(MessageID.GM_READY_BC, OnReadyBC);
        MessageDispatch.UnRegisterReceiver<GM_Begin>(MessageID.GM_BEGIN_BC, OnBeginBC);   
        MessageDispatch.UnRegisterReceiver<GM_Frame_BC>(MessageID.GM_FRAME_BC, OnFrameBC);
       
        #endregion

        #region Command
        CommandDispatch.UnRegisterReceiver<CMD_ReleaseSkill>(CommandID.RELEASE_SKILL, OnCommandReleaseSkill);
        CommandDispatch.UnRegisterReceiver<CMD_MoveToPoint>(CommandID.MOVE_TO_POINT, OnCommandMoveToPoint);
        CommandDispatch.UnRegisterReceiver<CMD_CreateMonster>(CommandID.CREATE_MONSTER, OnCreateMonster);
        #endregion
    }


    #region Message

    private void OnAccept(GM_Return recvData)
    {
        if(recvData==null)
        {
            return;
        }

        Client client = ClientService.GetSingleton().GetClient(ClientID.Frame);
        if(client!=null)
        {
            client.OnAccept(recvData.id);

            GM_Request sendData = new GM_Request();
            sendData.id = recvData.id;

            ClientService.GetSingleton().SendUdp(ClientID.Frame, MessageID.GM_ACCEPT_CS, sendData);
        }
    }
    private void OnConnectReturn(GM_Connect recvData)
    {
        int roleId = recvData.roleId;

        mFrameInterval = recvData.frameinterval;
        GameApplication.GetSingleton().mode = (Mode)recvData.mode;

        PlayerManager.GetSingleton().mRoleId = roleId;

        CreatePlayerCharacter(ProtoTransfer.Get(recvData.player));

        Debug.Log("自己连接成功,id = " + roleId);
    }

    private void OnConnectBC(GM_Connect recvData)
    {
        int roleId = recvData.roleId;

        CreatePlayerCharacter(ProtoTransfer.Get(recvData.player));

        Debug.Log("玩家连接成功, id = " + roleId);
    }

    /// <summary>
    /// 玩家断开连接
    /// </summary>
    /// <param name="recvData"></param>
    private void OnDisconnectBC(GM_Disconnect recvData)
    {
        if(recvData==null)
        {
            return;
        }
        PlayerManager.GetSingleton().RemovePlayerCharacter(recvData.roleId);

      
    }

    private void OnReadyBC(GM_Ready recvData)
    {

        Debug.Log("玩家准备,id=" + recvData.roleId);

        if (recvData == null) return;

        PlayerCharacter tmpPlayerCharacter = PlayerManager.GetSingleton().GetPlayerCharacter(recvData.roleId);
        if (tmpPlayerCharacter)
        {
            tmpPlayerCharacter.SetPosition(ProtoTransfer.Get(recvData.position));
            tmpPlayerCharacter.SetRotation(ProtoTransfer.Get(recvData.direction));

            tmpPlayerCharacter.SetReady();
        }
      
        EventDispatch.Dispatch(EventID.Ready_Broadcast, recvData.roleId);
        
    }

    private void OnBeginBC(GM_Begin recvData)
    {
        if (recvData.result == 0)
        {
            Debug.Log("Server frame begin");
        
            mCurrentFrame = 1;

            mBegin = true;

            mFrameTime = 0;
           
            mTickThread = new Thread(Tick);
            mTickThread.Start();

            EventDispatch.Dispatch(EventID.Begin_Broadcast, mBegin);

            CreateMonster();
         
        }
        else
        {
            if (Debuger.ENABLELOG)
                Debug.Log("Start result=" + recvData.result);
        }
    }

    private void OnFrameBC(GM_Frame_BC recvData)
    {
        if (GameApplication.GetSingleton().mode == Mode.LockStep)
        {
            OnLockStepFrameBC(recvData);
        }
        else{
            OnOptimisticFrameBC(recvData);
        }
    }
 
    #region LockStep

    /// <summary>
    /// 发送关键帧
    /// </summary>
    private void SendFrame()
    {
        if (mSentFrame >= mCurrentFrame)
        {
            return;
        }

        GM_Frame sendData = new GM_Frame();
        sendData.roleId = PlayerManager.GetSingleton().mRoleId;
        sendData.frame = mCurrentFrame;
        sendData.frametime = mFrameTime;
        lock (mCommandQueue)
        {
            while (mCommandQueue.Count > 0)
            {
                Command frame = mCommandQueue.Dequeue();
                GMCommand cmd = new GMCommand();
                cmd.id = 0;
                cmd.frame = frame.frame;
                cmd.type = frame.type;
                cmd.data = frame.data;
                cmd.frametime = frame.time;

                sendData.command.Add(cmd);
            }
            mCommandQueue.Clear();
        }

        ClientService.GetSingleton().SendUdp(ClientID.Frame, MessageID.GM_FRAME_CS, sendData);


        mSentFrame++;
    }
     
    private void OnLockStepFrameBC(GM_Frame_BC recvData)
    {
        if(recvData == null)
        {
            return;
        }
        //不打印那么多信息
        if (recvData.command.Count > 0 || recvData.frame % 30 ==0)
        {
            Debug.Log("Receive frame:" + recvData.frame + " command:" + recvData.command.Count);
        }

        long frame = recvData.frame;
      
        mFrameTime = recvData.frametime;

        if (frame == mCurrentFrame)
        {
            recvData.command.Sort(SortCommand);

            for (int i = 0; i < recvData.command.Count; ++i)
            {
                GMCommand cmd = recvData.command[i];
                Command data = new Command(cmd.id, cmd.frame,cmd.type, cmd.data, cmd.frametime );

                if (mFrameDic.ContainsKey(frame) == false)
                {
                    mFrameDic.Add(frame, new List<Command>());
                }
                mFrameDic[frame].Add(data);

                DoCommand(data);
            }

            EventDispatch.Dispatch(EventID.Frame_Broadcast, mCurrentFrame);

            mCurrentFrame++;
        }
        else
        {
            Debug.LogError("frame = " + frame + ",current=" + mCurrentFrame);
        }

    }
    #endregion
    #region 乐观模式
    /// <summary>
    /// 乐观模式处理
    /// </summary>
    /// <param name="recvData"></param>
    private void OnOptimisticFrameBC(GM_Frame_BC recvData)
    {
        if (recvData == null)
        {
            return;
        }

        mCurrentFrame = recvData.frame;

       
        //不打印那么多信息
        if (recvData.command.Count > 0 || recvData.frame % 30 == 0)
        {
            Debug.Log("Receive frame:" + recvData.frame + " command:" + recvData.command.Count);
            Debug.Log(recvData.frametime + "," + mFrameTime + ","+(mFrameTime - recvData.frametime));
        }

        mFrameTime = recvData.frametime;


        for (int i = 0; i < recvData.command.Count; ++i)
        {
            GMCommand cmd = recvData.command[i];
            Command data = new Command(cmd.id, cmd.frame, cmd.type, cmd.data, cmd.frametime );

            if (mFrameDic.ContainsKey(mCurrentFrame) == false)
            {
                mFrameDic.Add(mCurrentFrame, new List<Command>());
            }
            mFrameDic[mCurrentFrame].Add(data);

            DoCommand(data);
        }

        EventDispatch.Dispatch(EventID.Frame_Broadcast, mCurrentFrame);
    }
    #endregion
    #endregion

    /// <summary>
    /// 命令排序，确保每个客户端命令的执行顺序一致
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    int SortCommand(GMCommand a, GMCommand b)
    {
        //比较帧
        if (a.frame < b.frame)
        {
            return -1;
        }
        else if (a.frame > b.frame)
        {
            return 1;
        }
        else
        {
            //比较时间
            if (a.frametime < b.frametime)
            {
                return -1;
            }
            else if (a.frametime > b.frametime)
            {
                return 1;
            }
            else
            {
                //比较类型
                if (a.type < b.type)
                {
                    return -1;
                }
                else if (a.type > b.type)
                {
                    return 1;
                }
                else
                {
                    //比较数值
                    if (a.data.Length < b.data.Length)
                    {
                        return -1;
                    }
                    else if (a.data.Length > b.data.Length)
                    {
                        return 1;
                    }
                    else
                    {
                        int length = a.data.Length;
                        for (int i = 0; i < length; ++i)
                        {
                            if (a.data[i] < b.data[i])
                            {
                                return -1;
                            }
                            else if (a.data[i] > b.data[i])
                            {
                                return 1;
                            }
                        }
                    }
                }
            }
        }
        return 0;
    }



    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="cmd"></param>
    private void DoCommand(Command cmd)
    {
        CommandID id = (CommandID)cmd.type;
        if (Debuger.ENABLELOG)
            Debug.Log("执行关键帧 = " + id.ToString() + ",frametime = "+ cmd.time);

       

        CommandDispatch.Dispatch(cmd);

    }

    #region Command
    void OnCommandReleaseSkill(CMD_ReleaseSkill varCommand)
    {
        PlayerCharacter tmpPlayerCharacter = PlayerManager.GetSingleton().GetPlayerCharacter(varCommand.roleId);
        if (tmpPlayerCharacter)
        {
            tmpPlayerCharacter.ReleaseSkill(varCommand.skillId, ProtoTransfer.Get(varCommand.mouseposition));
        }
    }

    void OnCommandMoveToPoint(CMD_MoveToPoint varCommand)
    {
    
        PlayerCharacter tmpPlayerCharacter = PlayerManager.GetSingleton().GetPlayerCharacter(varCommand.roleId);
        if (tmpPlayerCharacter)
        {
            /*
            tmpPlayerCharacter.SetNavigation(false);
            tmpPlayerCharacter.transform.position = ProtoTransfer.Get(data.position);
            tmpPlayerCharacter.transform.rotation = Quaternion.Euler(ProtoTransfer.Get(data.direction));
            tmpPlayerCharacter.SetNavigation(true);
            */
            tmpPlayerCharacter.MoveToPoint(ProtoTransfer.Get(varCommand.destination));
        }
    }

    void OnCreateMonster(CMD_CreateMonster cmd)
    {
        if(cmd == null)
        {
            return;
        }

        CreatePlayerCharacter(ProtoTransfer.Get(cmd.player));

        PlayerCharacter tmpPlayerCharacter = PlayerManager.GetSingleton().GetPlayerCharacter(cmd.player.roleId);
        if(tmpPlayerCharacter)
        {
            tmpPlayerCharacter.SetPosition(ProtoTransfer.Get(cmd.position));
            tmpPlayerCharacter.SetRotation(ProtoTransfer.Get(cmd.direction));
            tmpPlayerCharacter.SetReady();
        }

    }
    #endregion


    void OnReadyRequest(int data)
    {
        PlayerCharacter tmpPlayerCharacter = PlayerManager.GetSingleton().mPlayerCharacterSelf;
        if (tmpPlayerCharacter)
        {
            Debug.Log("Try to Ready.");

            GM_Ready sendData = new GM_Ready();
            sendData.roleId = PlayerManager.GetSingleton().mRoleId;
            sendData.position = ProtoTransfer.Get(tmpPlayerCharacter.position);
            sendData.direction = ProtoTransfer.Get(tmpPlayerCharacter.direction);

            ClientService.GetSingleton().SendUdp(ClientID.Frame, MessageID.GM_READY_CS, sendData);
        }
        else
        {
            Debug.Log("人物没创建完成");
        }
    }

    void OnConnectRequest(EventConnect data)
    {
        Debug.Log("Request connect server.");

        ClientService.GetSingleton().Connect(ClientID.Frame,
            data.ip,
            data.tcpPort,
            data.udpPort,
            data.kcp,
            OnConnectSuccess, 
            OnConnectFail);
    }

    void OnAddCommand( Command cmd)
    {
        if (cmd == null) return;

        if(mBegin == false)
        {
            Debug.Log("Frame not Begin ");

            return;
        }

        Debug.Log("AddFrame " + (CommandID)cmd.type);

        cmd.SetFrame(mCurrentFrame, mFrameTime);

        if (GameApplication.GetSingleton().mode == Mode.LockStep)
        {
            lock (mCommandQueue)
            {
                mCommandQueue.Enqueue(cmd);
            }
        }
        else
        {
            //乐观模式马上发送命令

            GM_Frame sendData = new GM_Frame();
            sendData.roleId = PlayerManager.GetSingleton().mRoleId;
            sendData.frame = mCurrentFrame;
            sendData.frametime = mFrameTime;

            GMCommand data = ProtoTransfer.Get(cmd);

            sendData.command.Add(data);
            ClientService.GetSingleton().SendUdp(ClientID.Frame, MessageID.GM_FRAME_CS, sendData);

        }
    }


    #endregion

    void CreateMonster()
    {
        CMD_CreateMonster data = new CMD_CreateMonster();
        data.roleId = 0;
        data.player = new GMPlayerInfo();
        for (int i = 0; i < 5; ++i)
        {
            data.player.roleId = 10000+i;
            data.player.type = 1; //怪物
            data.player.moveSpeed = 350;
            data.player.maxBlood = 200;
            data.player.nowBlood = 200;
            data.player.name = "client " + data.player.roleId;
            data.position = ProtoTransfer.Get(new Vector3((i+1) * (i %2 == 0? -3:3), 1, 10));
            data.direction = ProtoTransfer.Get(Vector3.zero);

            Command cmd = new Command();
            cmd.Set(CommandID.CREATE_MONSTER, data);
            cmd.SetFrame(mCurrentFrame, mFrameTime);

            DoCommand(cmd);

        }

    }

    #region Other
    public static bool SamplePosition(Vector3 varOriginalPosition, out Vector3 varTargetPosition)
    {
        varTargetPosition = Vector3.zero;
        UnityEngine.AI.NavMeshHit hit;
        float tmpDistance = 0.05f;
        while ((tmpDistance *= 2) < 50)
        {

            if (UnityEngine.AI.NavMesh.SamplePosition(varOriginalPosition, out hit, tmpDistance, UnityEngine.AI.NavMesh.AllAreas))
            {

                varTargetPosition = hit.position;

                return true;
            }
        }
        return false;
    }
    #endregion
}


