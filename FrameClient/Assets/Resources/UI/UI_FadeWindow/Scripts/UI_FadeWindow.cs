using UnityEngine;
using System.Collections;

public class UI_FadeWindow : FadeWindow
{

	// Use this for initialization
	void Start () {
        Transform close = transform.Find("Close");
        
        UIEventListener.Get(close.gameObject).onClick = (go) => { Close(); };

        Transform main = transform.Find("MainWindow");

        UIEventListener.Get(main.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_Main>();
        };

        Transform scaleWindow = transform.Find("ScaleWindow");
        UIEventListener.Get(scaleWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_ScaleWindow>();
        };


        Transform moveWindow = transform.Find("MoveWindow");
        UIEventListener.Get(moveWindow.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_MoveWindow>();
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
