using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public GameObject Kagu;
    public Camera targetCamera;


    public void OnButtonClick()
    {
        Vector3 spawnPos = targetCamera.transform.position + targetCamera.transform.forward * 2f;
        Quaternion spawnRot = targetCamera.transform.rotation;

        Instantiate(Kagu, spawnPos, spawnRot);


    }
}

