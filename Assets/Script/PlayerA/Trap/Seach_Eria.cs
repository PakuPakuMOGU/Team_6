using UnityEngine;

public class Seach_Eria : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [SerializeField] public bool Seach { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag(playerTag))
        {
            Seach = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag(playerTag))
        {
            Seach = false;
        }
    }
}