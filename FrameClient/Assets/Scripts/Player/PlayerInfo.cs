using UnityEngine;
using System.Collections;



public class PlayerInfo
{
    public PlayerInfo()
    {
       
    }

	public int roleid;

    public string name;

	public int moveSpeed = 500;//基础移速
    public int moveSpeedAddition = 0;//移速加成
    public int moveSpeedPercent = 0;

    public int attackSpeed = 100;//基础攻速
	public int attackSpeedAddition = 0; // 攻速加成
    public int attackSpeedPercent = 0;

    public int maxBlood = 0;//最大血量
    public int nowBlood = 0;//当前血量

    public int type;

}



