using System;
using System.Collections.Generic;
using UnityEngine;

public class ScaleWindow : BaseWindow
{
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
    public override void OnResume()
    {
        base.OnResume();
        TweenScale tween = GetComponent<TweenScale>();
        if (tween == null) tween = gameObject.AddComponent<TweenScale>();
        tween.value = tween.tweenFactor > 0 ? tween.value : Vector3.zero;
        tween.from = tween.value;
        tween.to = Vector3.one;

        tween.duration = tween.value== Vector3.zero?duration: tween.value.magnitude * duration / Vector3.one.magnitude;

        tween.onFinished.Clear();
        tween.ResetToBeginning();
        tween.PlayForward();
    }

    public override void OnPause()
    {
        TweenScale tween = GetComponent<TweenScale>();
        if (tween == null) tween = gameObject.AddComponent<TweenScale>();
        tween.value = tween.tweenFactor > 0 ? tween.value : Vector3.one;
        tween.from = tween.value;
        tween.to = Vector3.zero;
        tween.duration = tween.value == Vector3.one ? duration : (Vector3.one- tween.value).magnitude * duration / Vector3.one.magnitude; 

        tween.ResetToBeginning();
        tween.onFinished.Clear();
        tween.onFinished.Add(new EventDelegate(delegate () { base.OnPause(); }));
        tween.PlayForward();
    }

    public override void OnExit()
    {
        TweenScale tween = GetComponent<TweenScale>();
        if (tween == null) tween = gameObject.AddComponent<TweenScale>();
        tween.value = tween.tweenFactor > 0 ? tween.value : Vector3.one;
        tween.from = tween.value;
        tween.to = Vector3.zero;
        tween.duration = tween.value == Vector3.one ? duration : (Vector3.one - tween.value).magnitude * duration / Vector3.one.magnitude; 

        tween.ResetToBeginning();
        tween.onFinished.Clear();
        tween.onFinished.Add(new EventDelegate(delegate () { base.OnExit(); }));
        tween.PlayForward();
    }

}

