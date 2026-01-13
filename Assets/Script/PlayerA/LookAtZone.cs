
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LookAtZone : MonoBehaviour
{
    public Transform parentToRotate;
    public Transform player;
    public float rotateSpeedDegPerSec = 180f;
    public bool lockYRotation = true;

    private bool _tracking = false;

    private void Awake()
    {
        if (parentToRotate == null) parentToRotate = transform.parent;
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _tracking = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _tracking = true; // ñàÉtÉåÅ[ÉÄONÇ…Ç∑ÇÈ
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _tracking = false;
        }
    }

    private void LateUpdate()
    {
        if (!_tracking || player == null || parentToRotate == null) return;

        Vector3 targetPos = player.position;
        if (lockYRotation) targetPos.y = parentToRotate.position.y;

        Vector3 dir = targetPos - parentToRotate.position;
        if (dir.sqrMagnitude <= 0f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        if (rotateSpeedDegPerSec <= 0f)
        {
            parentToRotate.rotation = targetRot;
        }
        else
        {
            parentToRotate.rotation = Quaternion.RotateTowards(
                parentToRotate.rotation,
                targetRot,
                rotateSpeedDegPerSec * Time.deltaTime
            );
        }
    }
}
