using System;
using UnityEngine;
using System.Collections.Generic;

public class SkillMove : MonoBehaviour
{
    public PlayerCharacter mPlayerCharacter;
    public Vector3 to;
    public float duration;

    private Vector3 from;
    private float time;

    void Start()
    {
        from = transform.position;
        time = duration;

        Helper.SetMeshRendererColor(transform, mPlayerCharacter.type == 0 ? Color.red : Color.blue);
    }
    void Update()
    {
        time -= Time.deltaTime;
        
        if(time < 0)
        {
            Destroy(gameObject);
        }
        else
        {
            float factor = time / duration;

            transform.position = from * factor + (1 - factor) * to;
        }   
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCharacter tmpPlayerCharacter = other.GetComponent<PlayerCharacter>();
        if(tmpPlayerCharacter && tmpPlayerCharacter.roleid != mPlayerCharacter.roleid)
        {
            int interval = -10; //伤害
            tmpPlayerCharacter.SetBlood(tmpPlayerCharacter.maxBlood, tmpPlayerCharacter.nowBlood + interval);

            if (tmpPlayerCharacter.nowBlood <= 0)
            {
                mPlayerCharacter.SetSpeed(mPlayerCharacter.moveSpeedAddition+ 50, mPlayerCharacter.moveSpeedPercent+ 20);
            }

            Destroy(gameObject);
        }
    }
}

