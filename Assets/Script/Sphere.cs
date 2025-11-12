using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public GameObject SphereObj;
    private bool finish = false;

    void Start()
    {
        // 設置時の処理は任せます.
    }

    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        // ダメージタグへの接触.
        if (other.gameObject.tag == "Damage" && !finish)
        {
            finish = true;
            GameFinish();
        }
    }

    private void GameFinish()
    {
        // ゲーム終了時の内容を記載.
        // インターネットで通知.
    }
}
