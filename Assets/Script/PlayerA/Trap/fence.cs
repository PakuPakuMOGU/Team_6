using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fence : MonoBehaviour
{
    static int Fence_HP;

    void Start()
    {
        

        if (gameObject.CompareTag("Fence1"))
        {
            Fence_HP = 200;
        }
        else if (gameObject.CompareTag("Fence2")) {
            Fence_HP = 500;
        }
    }
    void Update()
    {

        if (Fence_HP == 0) {
            Destroy(gameObject);
        }
    }
}
