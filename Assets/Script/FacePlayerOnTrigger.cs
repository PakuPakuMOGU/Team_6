
using UnityEngine;

/// <summary>
/// プレイヤーが後から生成されても、見つけ次第 その方向へ向く（距離＆FOV）
/// Collider不要。タグ or Providerから遅延取得に対応。
/// </summary>
public class FacePlayerOnRange_Lazy : MonoBehaviour
{
    [Header("プレイヤー参照（後から生成OK）")]
    [SerializeField] private Transform player;
    [Tooltip("プレイヤーを探すためのタグ名（例：Player）")]
    [SerializeField] private string playerTag = "Player";

    [Header("判定パラメータ")]
    [SerializeField, Min(0f)] private float triggerRadius = 5f;
    [SerializeField, Range(0f, 180f)] private float fovDegrees = 180f;

    [Header("回転設定")]
    [SerializeField] private bool rotateYAxisOnly = true;
    [SerializeField] private bool smoothRotate = true;
    [SerializeField, Min(0f)] private float rotateSpeed = 8f;

    [Header("ラインオブサイト（任意）")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask losMask = ~0;

    // プレイヤー探索のインターバル制御（毎フレーム探し続けるのを軽減）
    private float nextFindTime = 0f;
    private const float FindInterval = 0.5f; // 0.5秒おきに探索

    private void Update()
    {
        // 1) プレイヤー参照が未取得なら、一定間隔で探索
        if (player == null && Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + FindInterval;
            TryBindPlayer();
        }

        if (player == null) return;

        // 2) 距離判定
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude > triggerRadius * triggerRadius) return;

        // 3) FOV判定（必要な場合）
        if (fovDegrees < 180f)
        {
            Vector3 forward = transform.forward;
            if (rotateYAxisOnly)
            {
                forward.y = 0; forward.Normalize();
                Vector3 flatToPlayer = toPlayer; flatToPlayer.y = 0; flatToPlayer.Normalize();
                if (Vector3.Angle(forward, flatToPlayer) > fovDegrees * 0.5f) return;
            }
            else
            {
                if (Vector3.Angle(forward, toPlayer.normalized) > fovDegrees * 0.5f) return;
            }
        }

        // 4) LOS（遮蔽）チェック
        if (requireLineOfSight)
        {
            if (Physics.Linecast(transform.position, player.position, out var hit, losMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != player && hit.transform.root != player) return;
            }
        }

        // 5) 回転（Y軸のみ or 全軸）
        Quaternion targetRot;
        if (rotateYAxisOnly)
        {
            Vector3 flatDir = toPlayer; flatDir.y = 0;
            if (flatDir.sqrMagnitude < 1e-6f) return;
            targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        }
        else
        {
            targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        transform.rotation = smoothRotate
            ? Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed)
            : targetRot;
    }

    private void TryBindPlayer()
    {
        // 優先：タグで探索
        var go = GameObject.FindWithTag(playerTag);
        if (go != null)
        {
            player = go.transform;
            return;
        }

        // 代替：シーン上の PlayerProvider を探して参照をもらう
        var provider = FindFirstObjectByType<PlayerProvider>(FindObjectsInactive.Include);
        if (provider != null && provider.PlayerTransform != null)
        {
            player = provider.PlayerTransform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}

/// <summary>
/// プレイヤーをスポーンする側で、生成直後にここへセットしておくと、他のスクリプトが安全に参照できる。
/// </summary>
public class PlayerProvider : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }

    // 例：スポーン完了時に呼ぶ
    public void SetPlayer(Transform player)
    {
        PlayerTransform = player;
    }
}
