using UnityEngine;
using System.Collections.Generic;
using PBMessage;
using Network;
using UnityEngine.UI;

public class UI_Main : BaseWindow, IReceiverHandler
{

    public UI_Main() { mWindowType = WindowType.Root; }

    InputField mIP;
    InputField mTCP;
    InputField mUDP;
    Toggle mKCP;
    Text mPlayer;
    Text mFrame;

    Transform mConnect;
    Transform mReady;
    Transform mSkill1;
    Transform mSkill2;
    Transform mSkill3;
    Transform mTop;
    Transform mItem;

    Dictionary<int, GameObject> mItemDic = new Dictionary<int, GameObject>();

    private void Awake()
    {
        mIP = transform.Find("ip").GetComponent<InputField>();
        mTCP = transform.Find("tcp").GetComponent<InputField>();
        mUDP = transform.Find("udp").GetComponent<InputField>();
        mKCP = transform.Find("kcp").GetComponent<Toggle>();
        mPlayer = transform.Find("player").GetComponent<Text>();
        mFrame = transform.Find("frame").GetComponent<Text>();


        mIP.text = GameApplication.GetSingleton().ip;
        mTCP.text = GameApplication.GetSingleton().tcpPort.ToString();
        mUDP.text = GameApplication.GetSingleton().udpPort.ToString();
        mKCP.isOn = GameApplication.GetSingleton().kcp;
        mKCP.onValueChanged.AddListener(delegate (bool value) {
            GameApplication.GetSingleton().kcp = value;
        });

        mConnect = transform.Find("connect");
        mReady = transform.Find("ready");
        mSkill1 = transform.Find("skill1");
        mSkill2 = transform.Find("skill2");
        mSkill3 = transform.Find("skill3");
        mTop = transform.Find("top");
        mItem = transform.Find("top/item");

        mReady.gameObject.SetActive(false);
        mPlayer.gameObject.SetActive(false);
        mFrame.gameObject.SetActive(false);
        mItem.gameObject.SetActive(false);
        transform.Find("buttons").gameObject.SetActive(false);

        RegisterReceiver();
    }

    private void OnDestroy()
    {
        UnRegisterReceiver();
    }
    #region Event
    public void RegisterReceiver()
    {
        EventDispatch.RegisterReceiver<int>(EventID.Connect_Return, OnConnectReturn);
        EventDispatch.RegisterReceiver<int>(EventID.Ready_Broadcast, OnReadyBC);
        EventDispatch.RegisterReceiver<bool>(EventID.Begin_Broadcast, OnBeginBC);
        EventDispatch.RegisterReceiver<EventPlayerCreate>(EventID.Player_Create, OnPlayerCreate);
        EventDispatch.RegisterReceiver<EventPlayerMove>(EventID.Player_Move, OnPlayerMove);
        EventDispatch.RegisterReceiver<EventPlayerRemove>(EventID.Player_Remove, OnPlayerRemove);
        EventDispatch.RegisterReceiver<EventPlayerBloodChange>(EventID.Player_BloodChange, OnPlayerBloodChange);
        EventDispatch.RegisterReceiver<long>(EventID.Frame_Broadcast, OnFrameBC);
    }

    public void UnRegisterReceiver()
    {
        EventDispatch.UnRegisterReceiver<int>(EventID.Connect_Return, OnConnectReturn);
        EventDispatch.UnRegisterReceiver<int>(EventID.Ready_Broadcast, OnReadyBC);
        EventDispatch.UnRegisterReceiver<bool>(EventID.Begin_Broadcast, OnBeginBC);
        EventDispatch.UnRegisterReceiver<EventPlayerCreate>(EventID.Player_Create, OnPlayerCreate);
        EventDispatch.UnRegisterReceiver<EventPlayerMove>(EventID.Player_Move, OnPlayerMove);
        EventDispatch.UnRegisterReceiver<EventPlayerRemove>(EventID.Player_Remove, OnPlayerRemove);
        EventDispatch.UnRegisterReceiver<EventPlayerBloodChange>(EventID.Player_BloodChange, OnPlayerBloodChange);
        EventDispatch.UnRegisterReceiver<long>(EventID.Frame_Broadcast, OnFrameBC);
    }

    void OnConnectReturn(int result)
    {       
        if(result == 0)
        {
            mIP.gameObject.SetActive(false);
            mTCP.gameObject.SetActive(false);
            mUDP.gameObject.SetActive(false);
            mKCP.gameObject.SetActive(false);
            mConnect.gameObject.SetActive(false);
            mReady.gameObject.SetActive(true);
            mPlayer.gameObject.SetActive(true);
        }

        UpdatePlayerCount();
    }

    void OnReadyBC(int roleid)
    {
        if (roleid == PlayerManager.GetSingleton().mRoleId)
        {
            mConnect.gameObject.SetActive(false);
            mReady.gameObject.SetActive(false);
        }

        UpdatePlayerCount();

    }

    void OnBeginBC(bool result)
    {
        if(mFrame)
        {
            mFrame.gameObject.SetActive(true);
            transform.Find("buttons").gameObject.SetActive(true);
        }
    }

    void OnPlayerCreate(EventPlayerCreate data)
    {
        if(data == null)
        {
            return;
        }

        if(mItemDic.ContainsKey(data.roleid)==false)
        {
            GameObject go = Instantiate(mItem.gameObject);
            go.transform.SetParent(mTop);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);

            go.transform.Find("name").GetComponent<Text>().text = data.name;
            go.transform.Find("slider/value").GetComponent<Text>().text = string.Format("{0}/{1}", data.nowBlood, data.maxBlood);
            go.transform.Find("slider").GetComponent<Slider>().value = data.nowBlood * 1f / data.maxBlood;
            go.transform.Find("slider/Foreground").GetComponent<Image>().color = data.type == 0 ? new Color(0, 223f/255,1): Color.red;
         
            mItemDic.Add(data.roleid, go);

        }
        UpdatePlayerCount();
    }

    void OnPlayerMove(EventPlayerMove data)
    {
        if(data == null)
        {
            return;
        }
        if(PlayerManager.GetSingleton().mCamera==null)
        {
            return;
        }

        if(mItemDic.ContainsKey(data.roleid))
        {
            Vector3 screenPosition = PlayerManager.GetSingleton().mCamera.WorldToScreenPoint(data.position);

            float ratioX = WindowManager.DESIGN_WIDTH *1f / Screen.width;
            float ratioY = WindowManager.DESIGN_HEIGHT * 1f / Screen.height;

            screenPosition.x -= Screen.width * ratioX * 0.5f ;
            screenPosition.y -= Screen.height* ratioY * 0.5f;
            screenPosition.z = 0;

            screenPosition.y += 60;
            


            mItemDic[data.roleid].transform.localPosition = screenPosition;
        }
        UpdatePlayerCount();
    }

    void OnPlayerRemove(EventPlayerRemove data)
    {
        if(data==null)
        {
            return;
        }
        if(mItemDic.ContainsKey(data.roleid))
        {
            var go = mItemDic[data.roleid];
            mItemDic.Remove(data.roleid);
            Destroy(go);
        }

        UpdatePlayerCount();
    }

    void OnPlayerBloodChange(EventPlayerBloodChange data)
    {
        if(data ==null)
        {
            return;
        }
        if (mItemDic.ContainsKey(data.roleid))
        {
            var go = mItemDic[data.roleid];

            go.transform.Find("slider/value").GetComponent<Text>().text = string.Format("{0}/{1}", data.nowBlood, data.maxBlood);
            go.transform.Find("slider").GetComponent<Slider>().value = data.nowBlood * 1f / data.maxBlood;
        }
    }

    void UpdatePlayerCount()
    {
        if (mPlayer)
        {
            mPlayer.text = string.Format("Player:{0}/{1} Monster:{2}", PlayerManager.GetSingleton().GetReadyCount(0), PlayerManager.GetSingleton().GetCount(0),PlayerManager.GetSingleton().GetCount(1));
        }
    }

    void OnFrameBC(long frame)
    {
        if(mFrame)
        {
            mFrame.text = string.Format("frame:{0}", frame);
        }
    }
    #endregion

    // Use this for initialization
    void Start () {

        UIEventListener.Get(mConnect.gameObject).onClick = (go) => {
            RequestConnect();
        };

        UIEventListener.Get(mReady.gameObject).onClick = (go) =>
        {
            EventDispatch.Dispatch(EventID.Ready_Request, 0);
        };

      
        UIEventListener.Get(mSkill1.gameObject).onClick = (go) =>
        {
            ReleaseSkill(1);
        };
        UIEventListener.Get(mSkill2.gameObject).onClick = (go) =>
        {
            ReleaseSkill(2);
        };
        UIEventListener.Get(mSkill3.gameObject).onClick = (go) =>
        {
            ReleaseSkill(3);
        };


        Transform scaleWindow = transform.Find("buttons/ScaleWindow");
        UIEventListener.Get(scaleWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_ScaleWindow>();
        };

        Transform fadeWindow = transform.Find("buttons/FadeWindow");
        UIEventListener.Get(fadeWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_FadeWindow>();
        };
        Transform moveWindow = transform.Find("buttons/MoveWindow");
        UIEventListener.Get(moveWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_MoveWindow>();
        };

        Transform popWindow = transform.Find("buttons/PopWindow");
        UIEventListener.Get(popWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_PopWindow>();
        };
        Transform dialog = transform.Find("buttons/Dialog");

        UIEventListener.Get(dialog.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_Dialog>();
        };
    }


    void RequestConnect()
    {
        if(string.IsNullOrEmpty(mIP.text)||
            string.IsNullOrEmpty(mTCP.text)||
            string.IsNullOrEmpty(mUDP.text))
        {
            return;
        }

        EventConnect.sData.Clear();

        EventConnect.sData.ip = mIP.text;

        if (int.TryParse(mTCP.text, out EventConnect.sData.tcpPort)==false)
        {
            return;
        }
        if (int.TryParse(mUDP.text, out EventConnect.sData.udpPort) == false)
        {
            return;
        }
        EventConnect.sData.kcp = GameApplication.GetSingleton().kcp;

        EventDispatch.Dispatch(EventID.Connect_Request, EventConnect.sData);
    }

    void ReleaseSkill(int skillid)
    {
        if (PlayerManager.GetSingleton().mCamera == null)
        {
            return;
        }

        CMD_ReleaseSkill data = new CMD_ReleaseSkill();

        data.roleId = PlayerManager.GetSingleton().mRoleId;
        data.skillId = skillid;
        data.mouseposition = ProtoTransfer.Get(PlayerManager.GetSingleton().mousePosition);

        Command cmd = new Command();
        cmd.Set(CommandID.RELEASE_SKILL, data);

        EventDispatch.Dispatch(EventID.AddCommand, cmd);

    }
	// Update is called once per frame
	void Update () {
	
        if(Input.GetKeyDown(KeyCode.Q))
        {
            ReleaseSkill(1);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            ReleaseSkill(2);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ReleaseSkill(3);
        }
    }

   
}
