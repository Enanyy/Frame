using UnityEngine;
using System.Collections;

public class UI_MoveWindow : MoveWindow
{

    public UI_MoveWindow()
    {
        ///设置窗口从哪个方向移进
        mPivot = Pivot.Top;
    }
	// Use this for initialization
	void Start () {
        Transform close = transform.Find("Close");
        
        UIEventListener.Get(close.gameObject).onClick = (go) => { Close(); };

        Transform main = transform.Find("MainWindow");

        UIEventListener.Get(main.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_Main>();
        };

        Transform fadeWindow = transform.Find("FadeWindow");
        UIEventListener.Get(fadeWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_FadeWindow>();
        };

        Transform scaleWindow = transform.Find("ScaleWindow");
        UIEventListener.Get(scaleWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_ScaleWindow>();
        };

        Transform popWindow = transform.Find("PopWindow");
        UIEventListener.Get(popWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_PopWindow>();
        };
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
