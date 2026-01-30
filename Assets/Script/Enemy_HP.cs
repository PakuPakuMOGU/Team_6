using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_HP : MonoBehaviour
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
            kill(gameObject);
        }
    }

    public void kill(GameObject obj)
    {
        if (obj == null) return;

        // Destroy前に退避（安全）
        string t = obj.tag;

        switch (t)
        {
            case "Fence2":
            case "Fence1":
            case "Land":
            case "S_Robo":
            case "G_Robo":
            case "T_Robo":
            case "B_Tare":
            case "C_Tare":



            default:
                Debug.Log($"未対応タグ({t})：とりあえず破壊");
                break;
        }

        Destroy(obj);
    }
}



