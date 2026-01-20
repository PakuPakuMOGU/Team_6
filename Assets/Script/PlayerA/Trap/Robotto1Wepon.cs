using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Robotto1Weapon : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Player_HP>()?.TakeDamage(damage);
        }
    }
}

