using PBMessage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;
using UnityEngine.EventSystems;

public class PlayerManager : SingletonMono<PlayerManager>
{


    private List<PlayerCharacter> mPlayerCharacterList = new List<PlayerCharacter>();

    public int mRoleId;

    public PlayerCharacter mPlayerCharacterSelf;

    public Camera mCamera;

    private GameObject mClick;

    /// <summary>
    /// 上次点击的位置
    /// </summary>
    private Vector3 mLastPosition;
    public Vector3 mousePosition
    {
        get
        {
            return mLastPosition;
        }
    }

    public int GetReadyCount(int type)
    {
        int count = 0;
        for (int i = 0; i < mPlayerCharacterList.Count; ++i)
        {
            if (mPlayerCharacterList[i].ready && mPlayerCharacterList[i].type == type)
            {
                count++;
            }
        }
        return count;
    }

    public int GetCount(int type)
    {
        int count = 0;
        for (int i = 0; i < mPlayerCharacterList.Count; ++i)
        {
            if (mPlayerCharacterList[i].type == type)
            {
                count++;
            }
        }
        return count;
    }

    public void CreatePlayerCharacter(PlayerInfo varPlayerInfo, System.Action<PlayerCharacter> varCallback)
    {
        var obj = AssetManager.GetSingleton().Load<GameObject>("Player/Player"); //UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/R/Player/Player.prefab");

        GameObject go = Instantiate(obj) as GameObject;

        go.name = varPlayerInfo.roleid.ToString();

        go.transform.SetParent(transform);
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

       

        PlayerCharacter character = go.AddComponent<PlayerCharacter>();
        character.Init(varPlayerInfo);
      
       
        mPlayerCharacterList.Add(character);

        if(varPlayerInfo.roleid == mRoleId)
        {
            GameObject cameraGo = new GameObject("Main Camera");
          
            mCamera = cameraGo.AddComponent<Camera>();
            SmoothFollow smoothFollow = cameraGo.AddComponent<SmoothFollow>();
            smoothFollow.target = go.transform;
            smoothFollow.distance = 8;
            smoothFollow.height = 16;
            mPlayerCharacterSelf = character;
        }

        if(varCallback!=null)
        {
            varCallback(character);
        }
    }

    public PlayerCharacter GetPlayerCharacter(int roleId)
    {
        for(int i = 0; i < mPlayerCharacterList.Count; ++i)
        {
            if(mPlayerCharacterList[i].roleid == roleId)
            {
                return mPlayerCharacterList[i];
            }
        }
        return null;
    }

    public void RemovePlayerCharacter(int roleId)
    {
        for(int i = mPlayerCharacterList.Count - 1; i >=0; --i)
        {
            if(mPlayerCharacterList[i] == null || mPlayerCharacterList[i].roleid == roleId)
            {
                PlayerCharacter tmpPlayerCharacter = mPlayerCharacterList[i];
                mPlayerCharacterList.RemoveAt(i);
                Destroy(tmpPlayerCharacter.gameObject);

                EventPlayerRemove.sData.Clear();
                EventPlayerRemove.sData.roleid = roleId;
                EventDispatch.Dispatch(EventID.Player_Remove, EventPlayerRemove.sData);
            }
        }
    }

    public void OnUpdate()
    {
        for (int i = 0; i < mPlayerCharacterList.Count; ++i)
        {
            mPlayerCharacterList[i].OnUpdate();
        }

       
        if (Input.GetMouseButtonDown(1))
        {
            if(EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            if (mCamera && mPlayerCharacterSelf)
            {
                Ray tmpRay = mCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit tmpHit;
                if (Physics.Raycast(tmpRay, out tmpHit,100))
                {
                    mLastPosition.x = 0;
                    mLastPosition.y = 0;
                    mLastPosition.z = 0;

                    Vector3 position = tmpHit.point;

                    SetClickPosition(1, position);

                    CMD_MoveToPoint data = new CMD_MoveToPoint();
                    data.roleId = mRoleId;
                    data.destination = ProtoTransfer.Get(position);
                    data.position = ProtoTransfer.Get(mPlayerCharacterSelf.position);
                    data.direction = ProtoTransfer.Get(mPlayerCharacterSelf.direction);
                   

                    Command cmd = new Command();
                    cmd.Set(CommandID.MOVE_TO_POINT, data);

                    EventDispatch.Dispatch( EventID.AddCommand, cmd);

                }
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (mCamera && mPlayerCharacterSelf)
            {
                Ray tmpRay = mCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit tmpHit;
                if (Physics.Raycast(tmpRay, out tmpHit, 100))
                {
                    mLastPosition = tmpHit.point;
                    SetClickPosition(0, tmpHit.point);
                }
            }
        }

    }

    private void SetClickPosition(int type, Vector3 position)
    {
        if (mClick == null)
        {
            var obj = AssetManager.GetSingleton().Load<GameObject>("Effect/Click");
            if (obj)
            {
                mClick = Instantiate(obj);
            }
        }
        if (mClick)
        {
            mClick.transform.position = position;
            Helper.SetMeshRendererColor(mClick.transform, type == 1 ? Color.black : Color.red);

        }
    }

    public void Clear()
    {
       
    }

    void OnDestroy()
    {
        Clear();
    }
 
}


