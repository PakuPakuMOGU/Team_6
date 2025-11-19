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
            Bom.Play();
            Destroy(gameObject, 2f);
        }
    }
}
