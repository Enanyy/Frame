using UnityEngine;
using System.Collections;

public class UI_PopWindow : BaseWindow {

    public UI_PopWindow()
    {
        mWindowType = WindowType.Pop;
    }

    private void Start()
    {
        Transform close = transform.Find("Close");
        
        UIEventListener.Get(close.gameObject).onClick = (go) => { Close(); };

        Transform main = transform.Find("MainWindow");

        UIEventListener.Get(main.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_Main>();
        };

        Transform dialog = transform.Find("Dialog");

        UIEventListener.Get(dialog.gameObject).onClick = (go) =>
        {
            WindowManager.GetSingleton().Open<UI_Dialog>();
        };
    }
}
