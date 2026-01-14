using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HP : MonoBehaviour
{

    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp;

    private void Awake()
    {
        currentHp = maxHp;
    }

    // Robotto1Wepon から呼びたいメソッドをこのシグネチャで用意
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        // HPが0なら死亡処理など
        if (currentHp == 0)
        {
            Debug.Log("死");
        }
    }
}

