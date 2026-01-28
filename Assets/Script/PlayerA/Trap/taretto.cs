using UnityEngine;

public class taretto : MonoBehaviour
{
    [Header("対象")]
    public Transform player;

    [Header("挙動")]
    public float rotateSpeed = 8f;
    public bool lockYOnly = true;

    private Transform target;
    public bool isInside = false; // 監視用（読み取り専用運用を推奨）
    private int insideCount = 0;  // ★重なりカウント

    void Update()
    {
        if (!isInside || target == null) return;

        Vector3 toTarget = target.position - transform.position;
        if (lockYOnly) toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ★ ルートで判定（子コライダーでもOKにする）
        if (other.transform.root.CompareTag("Player"))
        {
            insideCount++;
            if (insideCount == 1) // 最初の進入
            {
                target = other.transform.root;
                isInside = true;
                // Debug.Log($"[taretto] Enter -> insideCount={insideCount}, isInside=true");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            insideCount = Mathf.Max(0, insideCount - 1);
            if (insideCount == 0)
            {
                isInside = false;
                target = null;
                // Debug.Log($"[taretto] Exit -> insideCount={insideCount}, isInside=false");
            }
            // else Debug.Log($"[taretto] Exit(partial) -> insideCount={insideCount}");
        }
    }

    // ★ 保険：自分や相手が無効化/破棄された場合でも確実にリセット
    private void OnDisable()
    {
        ResetInsideState();
    }

    private void OnDestroy()
    {
        ResetInsideState();
    }

    private void ResetInsideState()
    {
        insideCount = 0;
        isInside = false;
        target = null;
    }
}