using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PBMessage;
using Network;

public class PlayerCharacter:MonoBehaviour
{

    private PlayerInfo mPlayerInfo;

    public int roleid { get { return mPlayerInfo.roleid; } }
    public string name { get { return mPlayerInfo.name; } }
    public int type { get { return mPlayerInfo.type; } }
    public int maxBlood { get { return mPlayerInfo.maxBlood; } }
    public int nowBlood { get { return mPlayerInfo.nowBlood; } }
    public int moveSpeed { get { return (int)mNavMeshAgent.speed * 100; } }
    public Vector3 position { get { return transform.position; } }
    public Vector3 direction { get { return transform.rotation.eulerAngles; } }

    private bool mReady = false;

    public bool ready { get { return mReady; } }


    private NavMeshAgent mNavMeshAgent;
    private Vector3 mDestination;
    
  
    void Awake()
    {
        if (mNavMeshAgent == null)
        {
            mNavMeshAgent = GetComponent<NavMeshAgent>();
        }
        if(mNavMeshAgent== null)
        {
            mNavMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        mNavMeshAgent.acceleration = 1000;
        mNavMeshAgent.angularSpeed = 7200;

    }

    public void Init(PlayerInfo info)
    {
        mPlayerInfo = info;

        SetSpeed(mPlayerInfo.moveSpeedAddition, mPlayerInfo.moveSpeedPercent);

        SetColor();
    }

    void OnDestroy()
    {

    }

    public void OnUpdate()
    {

    }

    void LateUpdate()
    {
        EventPlayerMove.sData.roleid = roleid;
        EventPlayerMove.sData.position = position;

        EventDispatch.Dispatch(EventID.Player_Move, EventPlayerMove.sData);
    }

    public void SetReady()
    {
        mReady = true;
    }

    public void SetSpeed(int moveSpeedAddition, int moveSpeedPercent)
    {
        if(mPlayerInfo!=null)
        {
            mPlayerInfo.moveSpeedAddition = moveSpeedAddition;
            mPlayerInfo.moveSpeedPercent = moveSpeedPercent;

            mNavMeshAgent.speed = (mPlayerInfo.moveSpeed * (1 + mPlayerInfo.moveSpeedPercent * 0.01f) + mPlayerInfo.moveSpeedAddition) * 0.01f;
        }
    }

    public void SetNavigation(bool enable)
    { 
        if (mNavMeshAgent)
        {
            mNavMeshAgent.enabled = enabled;
        }
    }

    public void SetPosition(Vector3 position)
    {
        SetNavigation(false);
        transform.position = position;
        SetNavigation(true);
    }

    public void SetRotation(Vector3 angle)
    {
        SetNavigation(false);

        transform.rotation = Quaternion.Euler(angle);

        SetNavigation(true);

    }

    public void LookAt(Vector3 direction)
    {
        SetNavigation(false);

        transform.rotation = Quaternion.LookRotation(direction);

        SetNavigation(true);
    }

    public void SetBlood(int maxBlood, int nowBlood)
    {
        if (mPlayerInfo != null)
        {
            int interval = mPlayerInfo.nowBlood - nowBlood;

            mPlayerInfo.maxBlood = maxBlood;
            mPlayerInfo.nowBlood = nowBlood;

            EventPlayerBloodChange.sData.Clear();
            EventPlayerBloodChange.sData.roleid = roleid;
            EventPlayerBloodChange.sData.maxBlood = maxBlood;
            EventPlayerBloodChange.sData.nowBlood = nowBlood;
            EventPlayerBloodChange.sData.interval = interval;

            EventDispatch.Dispatch(EventID.Player_BloodChange, EventPlayerBloodChange.sData);

            if(nowBlood <=0)
            {
                PlayerManager.GetSingleton().RemovePlayerCharacter(roleid);
            }
        }
    }

    public void SetColor()
    {
        Color c = Color.yellow;
        if(type == 1) //怪物
        {
            c = Color.blue;
        }

        Helper.SetMeshRendererColor(transform, c);
     
    }
    

    public void MoveToPoint(Vector3 targetPosition)
    {
        mDestination = targetPosition;

        if(mNavMeshAgent && mNavMeshAgent.isOnNavMesh)
        {
            mNavMeshAgent.SetDestination(targetPosition);
        }
    }

    public void ReleaseSkill(int skillid, Vector3 mousePosition)
    {
        switch(skillid)
        {
            case 1:
                {
                    if (mousePosition != Vector3.zero)
                    {
                        mousePosition.y = position.y;

                        LookAt(mousePosition - position);
                    }

                    var obj = AssetManager.GetSingleton().Load<GameObject>("Effect/Bullet");
                    if(obj)
                    {
                        var go = Instantiate(obj);
                        go.transform.position = transform.position;
                        go.transform.rotation = transform.rotation;

                        SkillMove sm = go.AddComponent<SkillMove>();
                        sm.to = transform.position + transform.forward * 10;
                        sm.duration = .5f;
                        sm.mPlayerCharacter = this;
                    }

                }break;
            case 2:
                {
                    if (mousePosition != Vector3.zero)
                    {
                        mousePosition.y = position.y;

                        LookAt(mousePosition - position);
                    }

                    var obj = AssetManager.GetSingleton().Load<GameObject>("Effect/Bullet");
                    if (obj)
                    {
                        Quaternion r = transform.rotation;

                        for (int i = 0; i < 9; i++)  //循环求出各个点
                        {

                            Quaternion q = Quaternion.Euler(r.eulerAngles.x, r.eulerAngles.y - (10 * i)+45, r.eulerAngles.z); ///求出第i个点的旋转角度

                            Vector3 v = transform.position + (q * Vector3.forward) * 10;///该点的坐标

                            var go = Instantiate(obj);
                            go.transform.position = transform.position;
                            go.transform.rotation = q;

                            SkillMove sm = go.AddComponent<SkillMove>();
                            sm.to = v;
                            sm.duration = .5f;
                            sm.mPlayerCharacter = this;
                        }
                    }
                }
                break;
            case 3:
                {
                    var obj = AssetManager.GetSingleton().Load<GameObject>("Effect/Bullet");
                    if (obj)
                    {
                        Quaternion r = transform.rotation;

                        for (int i = 0; i < 36; i++)  //循环求出各个点
                        {
                            Quaternion q = Quaternion.Euler(r.eulerAngles.x, r.eulerAngles.y - (10 * i) , r.eulerAngles.z); ///求出第i个点的旋转角度

                            Vector3 v = transform.position + (q * Vector3.forward) * 10;///该点的坐标

                            var go = Instantiate(obj);
                            go.transform.position = transform.position;
                            go.transform.rotation = q;

                            SkillMove sm = go.AddComponent<SkillMove>();
                            sm.to = v;
                            sm.duration = .5f;
                            sm.mPlayerCharacter = this;
                        }
                    }
                }
                break;
        }
    }
}


