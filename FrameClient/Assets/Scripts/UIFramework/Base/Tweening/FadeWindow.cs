using System;
using System.Collections.Generic;
using UnityEngine;

public class FadeWindow : BaseWindow
{
    private  float mDuration = 1f;

    public float duration { get {
            if(mDuration<0)
            {
                mDuration = 1;
            }
            return mDuration;
        }
        set { mDuration = value; }
    }

    public override void OnResume()
    {
        TweenAlpha tween = GetComponent<TweenAlpha>();
        if (tween == null) tween = gameObject.AddComponent<TweenAlpha>();

        tween.value = tween.tweenFactor > 0 ? tween.value : 0;

        tween.from = tween.value;
        tween.to = 1;
        tween.duration = tween.value == 0 ?duration: tween.value * duration / 1f;


        tween.onFinished.Clear();
        tween.onFinished.Add(new EventDelegate(delegate () { base.OnResume(); }));
        tween.ResetToBeginning();
        tween.PlayForward();
    }

    public override void OnPause()
    {
        TweenAlpha tween = GetComponent<TweenAlpha>();
        if (tween == null) tween = gameObject.AddComponent<TweenAlpha>();

        tween.value = tween.tweenFactor > 0 ? tween.value : 0;
        tween.from = tween.value;
        tween.to = 0;
        tween.duration = tween.value == 1 ? duration : (1 - tween.value) * duration / 1f ;

        tween.onFinished.Clear();
        tween.onFinished.Add(new EventDelegate(delegate () { base.OnPause(); }));
        tween.ResetToBeginning();

        tween.PlayForward();
    }

    public override void OnExit()
    {
        TweenAlpha tween = GetComponent<TweenAlpha>();
        if (tween == null) tween = gameObject.AddComponent<TweenAlpha>();

        tween.value = tween.tweenFactor > 0 ? tween.value : 0;
        tween.from = tween.value;
        tween.to = 0;
        tween.duration = tween.value == 1 ? duration : (1 - tween.value) * duration / 1f;


        tween.onFinished.Clear();
        tween.onFinished.Add(new EventDelegate(delegate () { base.OnExit(); }));
        tween.ResetToBeginning();

        tween.PlayForward();
    }
}

