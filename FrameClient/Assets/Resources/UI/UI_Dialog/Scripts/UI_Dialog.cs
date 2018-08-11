using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Dialog : BaseWindow
{
    UI_Dialog()
    {
        mWindowType = WindowType.Pop;
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
    }
	
	
}
