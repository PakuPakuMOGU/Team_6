
using UnityEngine;

public class taretto : MonoBehaviour
{
    [Header("対象")]
    public Transform player;           // プレイヤーのTransform（Inspectorで割当 or 自動取得）

    [Header("挙動")]
    public float detectionRadius = 8f; // 反応半径
    public float rotateSpeed = 8f;     // 回転スピード
    public bool lockYOnly = true;      // Y軸だけ回転（水平面）

    void Start()
    {
        // Inspectorで未設定ならタグ"Player"から探す（任意）
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        // 範囲チェック
        if (toPlayer.sqrMagnitude > detectionRadius * detectionRadius) return;

        if (lockYOnly) toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
