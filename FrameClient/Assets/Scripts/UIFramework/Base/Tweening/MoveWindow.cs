using System;
using System.Collections.Generic;
using UnityEngine;
public class MoveWindow : BaseWindow
{
    public enum Pivot
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Left,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
    }

    private float mDuration = 0.3f;

    public float duration
    {
        get
        {
            if (mDuration < 0)
            {
                mDuration = 1;
            }
            return mDuration;
        }
        set { mDuration = value; }
    }

    protected Pivot mPivot;
    public Pivot pivot
    {
        get { return mPivot; }
    }

    private Vector3 GetPivot(Pivot pivot)
    {
        Vector3 pos = Vector3.zero;

       
    

        switch(pivot)
        {
            case Pivot.Top:
                {
                    pos.y = Screen.height;
                }break;
            case Pivot.Bottom:
                {
                    pos.y = -Screen.height;
                }break;
            case Pivot.Left:
                {
                    pos.x = -Screen.width;
                }break;
            case Pivot.Right:
                {
                    pos.x = Screen.width;
                }
                break;
            case Pivot.TopLeft:
                {
                    pos.x = -Screen.width;
                    pos.y = Screen.height;
                }break;
            case Pivot.TopRight:
                {
                    pos.x = Screen.width;
                    pos.y = Screen.height;
                }
                break;
            case Pivot.BottomLeft:
                {
                    pos.x = -Screen.width;
                    pos.y = -Screen.height;
                }
                break;
            case Pivot.BottomRight:
                {
                    pos.x = Screen.width;
                    pos.y = -Screen.height;
                }
                break;
        }
        return pos;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Vector3 from = GetPivot(pivot);
        transform.localPosition = from;
    }

    public override void OnResume()
    {
        base.OnResume();

        TweenPosition tween = GetComponent<TweenPosition>();
        if (tween == null) tween = gameObject.AddComponent<TweenPosition>();

        Vector3 from = GetPivot(pivot);

        tween.value = tween.tweenFactor > 0 ? tween.value : from;

        tween.from = tween.value;
        tween.to = Vector3.zero;
        tween.duration = tween.value == from? duration : (from - tween.value).magnitude * duration / from.magnitude ;


        tween.onFinished.Clear();

        tween.ResetToBeginning();

        tween.PlayForward();
    }

    public override void OnPause()
    {

        TweenPosition tween = GetComponent<TweenPosition>();
        if (tween == null) tween = gameObject.AddComponent<TweenPosition>();

        Vector3 to = GetPivot(pivot);

        tween.value = tween.tweenFactor > 0 ? tween.value : Vector3.zero;
        tween.from = tween.value;
        tween.to = to;
        tween.duration = tween.value == Vector3.zero ? duration : tween.value.magnitude * duration / to.magnitude;

        tween.onFinished.Clear();

        tween.onFinished.Add(new EventDelegate(delegate () { base.OnPause(); }));

        tween.ResetToBeginning();

        tween.PlayForward();
    }

    public override void OnExit()
    {
        TweenPosition tween = GetComponent<TweenPosition>();
        if (tween == null) tween = gameObject.AddComponent<TweenPosition>();


        Vector3 to = GetPivot(pivot);

        tween.value = tween.tweenFactor > 0 ? tween.value : Vector3.zero;
        tween.from = tween.value;
        tween.to = to;
        tween.duration = tween.value == Vector3.zero ? duration : tween.value.magnitude * duration / to.magnitude;

        tween.onFinished.Clear();

        tween.onFinished.Add(new EventDelegate(delegate () { base.OnExit(); }));

        tween.ResetToBeginning();

        tween.PlayForward();
    }
}

