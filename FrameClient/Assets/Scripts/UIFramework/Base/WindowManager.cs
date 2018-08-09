using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    public const int DESIGN_WIDTH = 1280;
    public const int DESIGN_HEIGHT = 720;

    static WindowManager mInstance;
    public static WindowManager GetSingleton()
    {
        if(mInstance == null)
        {
            GameObject go = new GameObject(typeof(WindowManager).ToString());
            DontDestroyOnLoad(go);
            mInstance = go.AddComponent<WindowManager>();

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.SetParent(go.transform);
            DontDestroyOnLoad(canvas);
            RectTransform rectTrans = canvas.AddComponent<RectTransform>();
            rectTrans.anchorMax = Vector2.one * 0.5f;
            rectTrans.anchorMin = Vector2.one * 0.5f;
            rectTrans.pivot = Vector2.zero;
            rectTrans.localScale = Vector3.one;
           

            mCanvas = canvas.AddComponent<Canvas>();
            mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mCanvas.pixelPerfect = false;
            mCanvas.sortingOrder = 0;

            CanvasScaler canvasScaler = canvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(DESIGN_WIDTH, DESIGN_HEIGHT);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0;
            canvasScaler.referencePixelsPerUnit = 100;


            GraphicRaycaster raycaster = canvas.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.TwoD;

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.transform.SetParent(go.transform);

            EventSystem eventSystem = eventSystemGo.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 5;


            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        return mInstance;
    }

    static Canvas mCanvas;
   

    public static void SetTouchable(bool touchable)
    {
    }

    private Stack<BaseWindow> mWindowStack = new Stack<BaseWindow>();
    private Stack<BaseWindow> mTmpWindowStack = new Stack<BaseWindow>();

    public void Open<T>(Action<T> callback = null) where T : BaseWindow
    {
        SetTouchable(false);

        T t = Get<T>();

        if (t)
        {
            mTmpWindowStack.Clear();

            while (mWindowStack.Count > 0)
            {
                BaseWindow window = mWindowStack.Pop();
                if (window == t)
                {
                    break;
                }
                else
                {
                    mTmpWindowStack.Push(window);
                }
            }

            while (mTmpWindowStack.Count > 0)
            {
                BaseWindow window = mTmpWindowStack.Pop();

                SetLayer(window);

                mWindowStack.Push(window);
            }

            mTmpWindowStack.Clear();

            Push(t, callback);
        }
        else
        {
            string path = WindowPath.Get<T>();

            if (string.IsNullOrEmpty(path) == false)
            {
                Load(path, (asset) => {

                    if (asset)
                    {
                        GameObject go = Instantiate(asset) as GameObject;

                        Transform tran = go.transform.Find(typeof(T).ToString());

                        tran.SetParent(mCanvas.transform);

                        Destroy(go);

                        tran.localPosition = Vector3.zero;
                        tran.localRotation = Quaternion.identity;
                        tran.localScale = Vector3.one;

                        tran.gameObject.SetActive(true);


                        t = tran.GetComponent<T>();

                        if (t == null) t = tran.gameObject.AddComponent<T>();

                        if (t.windowType == WindowType.Root)
                        {
                            BaseWindow window = Get(WindowType.Root);

                            if (window != null)
                            {
                                Destroy(tran.gameObject);
                                SetTouchable(true);

                                if (callback != null) callback(null);

                                return;
                            }
                        }

                        t.path = path;

                        t.OnEnter();

                        Push(t, callback);
                    }
                    else
                    {
                        SetTouchable(true);
                    }

                });
            }
            else
            {
                SetTouchable(true);
            }
        }
    }

    private void Push<T>(T t, Action<T> callback) where T : BaseWindow
    {
        if (t)
        {
            if (mWindowStack.Count > 0)
            {
                //打开Root 关闭其他的
                if (t.windowType == WindowType.Root)
                {
                    while (mWindowStack.Count > 0)
                    {
                        BaseWindow window = mWindowStack.Pop();

                        if (window)
                        {
                            if (window != t)
                            {
                                window.OnExit();
                            }
                        }
                    }
                }
                else if(t.windowType == WindowType.Pop)
                {

                }
                else
                {
                    BaseWindow window = mWindowStack.Peek();

                    if (window && window.isPause == false)
                    {
                        window.OnPause();
                    }
                }
            }

            SetLayer(t);

            mWindowStack.Push(t);

            t.OnResume();
        }

        SetTouchable(true);

        if (callback != null)
        {
            callback(t);
        }
    }

    public T Get<T>()where T :BaseWindow
    {
        if(mWindowStack == null)
        {
            mWindowStack = new Stack<BaseWindow>();
        }

        var it = mWindowStack.GetEnumerator();

        while(it.MoveNext())
        {
            Type type = it.Current.GetType();
            if(type == typeof(T))
            {
                return it.Current as T;
            }
        }

        return null;
    }

    /// <summary>
    /// 关闭最上面的UI,不会关闭Root窗口
    /// </summary>
    public void Close()
    {
        if (mWindowStack == null) return;

        if(mWindowStack.Count > 0)
        {
            SetTouchable(false);

            BaseWindow window = mWindowStack.Pop();

            if(window && window.windowType != WindowType.Root)
            {
                window.OnExit();
            }

       
            if(mWindowStack.Count >0)
            {
                window = mWindowStack.Peek();

                if (window && window.isPause)
                {
                    window.OnResume();
                }
            }

            SetTouchable(true);
        }
    }

    public void Close<T>() where T : BaseWindow
    {
        if (mWindowStack == null) return;
        if (mTmpWindowStack == null) mTmpWindowStack = new Stack<BaseWindow>();

        SetTouchable(false);

        while (mWindowStack.Count > 0)
        {
            if (mWindowStack.Peek().GetType() != typeof(T))
            {
                mTmpWindowStack.Push(mWindowStack.Pop());
            }
            else
            {
                BaseWindow window = mWindowStack.Pop();

                if(window)
                {
                    window.OnExit();
                }
            }
        }

        while(mTmpWindowStack.Count>0)
        {
            mWindowStack.Push(mTmpWindowStack.Pop());
        }

        if (mWindowStack.Count > 0)
        {
            BaseWindow  window = mWindowStack.Peek();

            if (window && window.isPause)
            {
                window.OnResume();
            }
        }

        SetTouchable(true);
    }


    private BaseWindow Get(WindowType windowType) 
    {
        var it = mWindowStack.GetEnumerator();

        while(it.MoveNext())
        {
            if(it.Current.windowType == windowType)
            {
                return it.Current;
            }
        }
        return null;
    }

    private void SetLayer(BaseWindow window)
    {
        if(window)
        {
            if(mWindowStack.Count > 0)
            {
                BaseWindow t = mWindowStack.Peek();

                //window.panel.depth = t.panel.depth + 50;
            }
            else
            {
                //window.panel.depth = 100;
            }
        }
    }

    /// <summary>
    /// 暂停所有窗口
    /// </summary>
    public void Hide()
    {
        var it = mWindowStack.GetEnumerator();

        while(it.MoveNext())
        {
            if (it.Current.isPause == false)
            {
                it.Current.OnPause();
            }
        }
    }

    /// <summary>
    /// 显示栈顶的窗口
    /// </summary>
    public void Show()
    {
       if(mWindowStack.Count > 0)
        {
            BaseWindow window = mWindowStack.Peek();
            if(window && window.isPause)
            {
                window.OnResume();

                if(window.windowType == WindowType.Pop)
                {
                    if(mWindowStack.Count >=2)
                    {
                        window = mWindowStack.Pop();

                        BaseWindow secondWindow = mWindowStack.Peek();

                        if(secondWindow && secondWindow.isPause)
                        {
                            secondWindow.OnResume();
                        }

                        mWindowStack.Push(window);
                    }
                }
            }
        }
    }

    public void CloseAll()
    {
        while(mWindowStack.Count>0)
        {
            BaseWindow window = mWindowStack.Pop();

            if(window)
            {
                window.OnExit();
            }
        }
        mWindowStack.Clear();
    }

    private void Load(string path, Action<UnityEngine.Object> callback)
    {
       
        var tmpObject = AssetManager.GetSingleton().Load<UnityEngine.Object>(path);
        if (callback != null)
        {
            callback(tmpObject);
        }
    }

   

}

