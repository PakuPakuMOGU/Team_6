using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Landwin : MonoBehaviour
{
    public ParticleSystem Bom;


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("’n—‹ÚGI");
            Bom.Play();
            Destroy(gameObject, 2f);
        }
    }
}
