using UnityEngine;
using System.Collections;

public class BaseWindow : MonoBehaviour {

    private string mPath;

    /// <summary>
    /// 界面预设路径
    /// </summary>
    public string path { get { return mPath; } set { mPath = value; } }

    private bool mPause = false;
    public bool isPause { get { return mPause; } }

    protected WindowType mWindowType = WindowType.Normal;
    public WindowType windowType { get { return mWindowType; } }

    

    private GameObject mMask;
    public GameObject mask
    {
        get {
            if (mMask == null) CreateMask();
            return mMask;
        }
    }

   void CreateMask()
    {
        GameObject go = new GameObject("Mask");
        go.transform.SetParent(transform);
        go.transform.SetAsFirstSibling();
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        mMask = go;

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;

    }


    /// <summary>
    /// 界面被创建出来，只在创建完成调用一次
    /// </summary>
    public virtual void OnEnter()
    {
        CreateMask();
    }

    /// <summary>
    /// 界面暂停
    /// </summary>
    public virtual void OnPause()
    {
       
        mPause = true;
    }

    /// <summary>
    /// 界面继续
    /// </summary>
    public virtual void OnResume()
    {
        mPause = false;     
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// 界面退出，只在界面被销毁时调用一次
    /// </summary>
    public virtual void OnExit()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 不需要主动调用，绑定到界面的关闭或返回按钮就行
    /// </summary>
    protected virtual void Close()
    {
        WindowManager.GetSingleton().Close();
    }
}
