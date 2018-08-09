using UnityEngine;
using System;
using System.Collections;

public class StateMachine  {

	private State mCurrentState;
	private State mPreviousState;

	public StateMachine()
	{
		mCurrentState = null;
		mPreviousState = null;
	}

	/*状态改变*/
	public bool ChangeState (State state)
	{
		if (state == null) {
	
			Debug.LogError ("can't find this state");

			return false;
		}

		/*if (IsState (state.GetState ())) {
			return;
		}*/

		//触发退出状态调用Exit方法
		if (mCurrentState != null) {
			mCurrentState.OnExit ();
		}
		//保存上一个状态 
		mPreviousState = mCurrentState;
		//设置新状态为当前状态
		mCurrentState = state;

		mCurrentState.SetStateMachine (this);

		//进入当前状态调用Enter方法
		mCurrentState.OnEnter ();

		return true;

		//Debug.Log ("StateMachine enter State:"+mCurrentState.GetType().ToString());
	}
	/*Update*/
	public virtual void OnUpdate ()
	{
		
		if (mCurrentState != null)
			mCurrentState.OnUpdate ();
	}

	public void RevertPreviousState ()
	{
		//切换到前一个状态
		ChangeState (mPreviousState);
	}

	public State GetCurrentState ()
	{
		//返回当前状态
		return mCurrentState;
	}

	public State GetPreviousState ()
	{
		//返回前一个状态
		return mPreviousState;
	}
		
}
