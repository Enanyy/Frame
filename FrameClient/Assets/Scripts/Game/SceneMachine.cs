using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneMachine:Singleton<SceneMachine>
{
	Dictionary<GameSceneType, GameScene> mGameSceneDic = new Dictionary<GameSceneType, GameScene>();

	StateMachine mSceneStateMachine = new StateMachine ();



	public void Init()
	{
        RegisterGameScene(GameSceneType.FrameScene, new FrameScene());
    }

    bool RegisterGameScene(GameSceneType varSceneType, GameScene varScene)
	{
		if (!mGameSceneDic.ContainsKey (varSceneType)) {
	
			mGameSceneDic.Add (varSceneType, varScene);
			return true;
		}
		return false;
	}


	public void OnUpdate()
	{
		mSceneStateMachine.OnUpdate ();	

	}

    public void Destroy()
    {
        if(mSceneStateMachine!=null)
        {
            State tmpScene =   mSceneStateMachine.GetCurrentState();
            if(tmpScene!=null)
            {
                tmpScene.OnExit();
            }
        }
    }

	public GameScene currentScene {get{ return mSceneStateMachine.GetCurrentState () as GameScene;}}

	public GameSceneType currentSceneType{
		get{ 
			if (currentScene != null) {
				return currentScene.sceneType;
			}
			return GameSceneType.None;
		}
	}

    public void ChangeScene(GameSceneType varSceneType)
    {
        if (!mGameSceneDic.ContainsKey(varSceneType))
        {

            if (Debuger.ENABLELOG)
                Debug.LogError("The scene " + varSceneType + " is not register.");
            return;
        }

        GameScene tmpCurrentScene = mSceneStateMachine.GetCurrentState() as GameScene;

        GameScene tmpGotoScene = mGameSceneDic[varSceneType];

        if (tmpGotoScene == tmpCurrentScene || tmpGotoScene == null)
        {
            return;
        }


        mSceneStateMachine.ChangeState(tmpGotoScene);


    }


}


