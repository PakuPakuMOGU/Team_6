using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public GameObject Kagu;
    public Camera targetCamera;

    public void OnButtonClick()
    {
        Camera cam = targetCamera;
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 2f;

        RaycastHit hit;
        // ã‚©‚ç‰º‚ÉRay‚ğ”ò‚Î‚µ‚Ä’n–Ê‚ğ’T‚·
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            spawnPos = hit.point; // ’n–Ê‚ÌˆÊ’u‚É‡‚í‚¹‚é
        }

        Instantiate(Kagu, spawnPos, Quaternion.identity);
    }




}

