using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sphere : MonoBehaviour
{
    // スフィア側につけてください.
    public View viewScA;
    public View viewScB;
    private bool finish = false;
    private bool winATag = false;

    void Start()
    {
        //GameFinish();
    }

    // スフィアへの衝突判定.
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Damage" && !finish)
        {
            finish = true;
            GameFinish();
        }
    }

    private void GameFinish()
    {
        // インターネット通知入れる.

        if (winATag)
        {
            viewScA.WindowView();
        }
        else
        {
            viewScB.WindowView();
        }
    }
}