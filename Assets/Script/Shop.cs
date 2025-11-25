using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public GameObject Kagu;

    public void OnButtonClick()
    {
            Camera cam = Camera.main;

            Vector3 spawnPos = cam.transform.position + cam.transform.forward * 2f;

            Quaternion spawnRot = cam.transform.rotation;

            Instantiate(Kagu, spawnPos, spawnRot);
        
    }
}

