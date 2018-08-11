//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2016 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Tween the object's alpha. Works with both UI widgets as well as renderers.
/// </summary>

[AddComponentMenu("Tween/Tween Alpha")]
public class TweenAlpha : UITweener
{
	[Range(0f, 1f)] public float from = 1f;
	[Range(0f, 1f)] public float to = 1f;

	bool mCached = false;
    CanvasGroup mCanvasGroup;
	
	void Cache ()
	{
		mCached = true;

        mCanvasGroup = GetComponent<CanvasGroup>();

		if (mCanvasGroup == null)
		{
            mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
		}
	}

	/// <summary>
	/// Tween's current value.
	/// </summary>

	public float value
    {
        get
        {
            if (!mCached) Cache();

            return mCanvasGroup.alpha;


        }
        set
        {
            if (!mCached) Cache();

            mCanvasGroup.alpha = value;

        }
    }
	/// <summary>
	/// Tween the value.
	/// </summary>

	protected override void OnUpdate (float factor, bool isFinished) { value = Mathf.Lerp(from, to, factor); }

	/// <summary>
	/// Start the tweening operation.
	/// </summary>

	static public TweenAlpha Begin (GameObject go, float duration, float alpha)
	{
		TweenAlpha comp = UITweener.Begin<TweenAlpha>(go, duration);
		comp.from = comp.value;
		comp.to = alpha;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}

	public override void SetStartToCurrentValue () { from = value; }
	public override void SetEndToCurrentValue () { to = value; }
}
