
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChildTriggerForwarder : MonoBehaviour
{
    public ParentLookAtController parentController;
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (parentController == null)
            parentController = GetComponentInParent<ParentLookAtController>();

        var col = GetComponent<Collider>();
        if (col && !col.isTrigger)
            Debug.LogWarning($"{name}: Collider.isTrigger ‚ğ ON ‚É‚µ‚Ä‚­‚¾‚³‚¢B");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            parentController?.NotifyEnter(other);
    }

    // ‹«ŠEƒMƒŠƒMƒŠ‚â•¡”Collider‚Å‚Ìæ‚è‚±‚Ú‚µ‘Îô
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag))
            parentController?.NotifyEnter(other); // HashSet‚È‚Ì‚Åd•¡’Ç‰ÁOKi–³ŠQj
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            parentController?.NotifyExit(other);
    }
}
