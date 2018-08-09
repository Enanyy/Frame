using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Xml;
using System.Security;
using PBMessage;

public class GameScene :State{

	private GameSceneType mSceneType;

	public GameSceneType sceneType { get{ return mSceneType;}}

	
	
	public GameScene(GameSceneType varSceneType):base(varSceneType.ToString())
	{
		mSceneType = varSceneType;
	}

    public override void OnEnter()
    {
        SceneManager.LoadScene(name);
       
    }

	public override void OnUpdate ()
	{
		base.OnUpdate ();
		
	}

	public override void OnExit ()
	{
		

		base.OnExit ();
	
	}


}
