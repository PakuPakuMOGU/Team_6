using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Otosiana : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void KanbotuTime()
    {
        

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("óéÇ∆Çµåäê⁄êGÅI");
            KanbotuTime();
            Destroy(gameObject, 2f);
        }
    }
}
