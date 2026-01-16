
using UnityEngine;
using System.Collections;

/// <summary>
/// プレイヤーが detectRadius 内にいる間だけ、銃口の前方（firePoint.forward）にまっすぐ Ray を撃つ。
/// 当たったコライダーの Root が Player タグなら Player_HP にダメージを与える。
/// 既存の「向く」スクリプトは別で動いている前提（本スクリプトは回頭しない）。
/// </summary>
public class EnemyStraightRayShooter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("銃口Transform（Z+が前）。未指定なら this.transform を使用")]
    public Transform firePoint;
    [Tooltip("プレイヤーTransform。未指定なら Tag=Player から自動取得")]
    public Transform player;

    [Header("発砲条件")]
    [Tooltip("この半径内にプレイヤーがいれば発砲判定を行う")]
    public float detectRadius = 15f;
    [Tooltip("プレイヤーへ視線が通っている時のみ撃つ（遮蔽物越しに撃たなくなる）")]
    public bool requireLineOfSight = false;
    [Tooltip("前方±この角度内にプレイヤーがいる時のみ撃つ（“向けてる時だけ撃つ”用）")]
    [Range(0f, 180f)] public float viewHalfAngle = 0f; // 0 なら無効

    [Header("射撃設定")]
    [Tooltip("連射間隔（秒）")]
    public float shootInterval = 0.2f;
    [Tooltip("Ray の長さ（レーザーの見た目の長さにも使用）")]
    public float rayDistance = 30f;
    [Tooltip("1回のヒットで与えるダメージ")]
    public int damage = 10;
    [Tooltip("ヒット対象のレイヤー（Player と 遮蔽物を含め、Enemy は除外）")]
    public LayerMask hitMask;

    [Header("VFX（任意）")]
    [Tooltip("命中地点で生成するエフェクト")]
    public GameObject hitImpactPrefab;
    [Tooltip("弾筋表示用（LineRendererなど）。短寿命のPrefabを想定")]
    public GameObject tracerPrefab;

    [Header("デバッグ")]
    public bool debugLogs = false;
    public bool drawGizmos = true;

    Coroutine _loop;
    WaitForSeconds _wait;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }
        if (!firePoint) firePoint = transform; // 念のため
        _wait = new WaitForSeconds(shootInterval);
    }

    void OnEnable()
    {
        _loop = StartCoroutine(ShootLoop());
    }

    void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
    }

    IEnumerator ShootLoop()
    {
        while (true)
        {
            if (CanShootNow())
            {
                ShootForward();
            }
            yield return _wait;
        }
    }

    /// <summary>
    /// 発砲できる状態かを判定（距離 / FOV / 視線）
    /// </summary>
    bool CanShootNow()
    {
        if (!player || !firePoint) return false;

        // 1) 距離チェック（高速化のため2乗距離）
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude > detectRadius * detectRadius) return false;

        // 2) 視野チェック（任意：viewHalfAngle > 0 の時だけ適用）
        if (viewHalfAngle > 0f)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f;
            Vector3 toP = toPlayer; toP.y = 0f;
            if (toP.sqrMagnitude > 0.0001f)
            {
                float ang = Vector3.Angle(fwd.normalized, toP.normalized);
                if (ang > viewHalfAngle) return false;
            }
        }

        // 3) 視線チェック（任意）
        if (requireLineOfSight && !HasLineOfSight()) return false;

        return true;
    }

    /// <summary>
    /// 銃口からプレイヤーに向けてRayを飛ばし、最初に当たるのがPlayerなら視線OK
    /// （“撃つ方向”は firePoint.forward のまま／ここは視線チェックのためだけに使用）
    /// </summary>
    bool HasLineOfSight()
    {
        Vector3 origin = firePoint.position;
        Vector3 dirToPlayer = (player.position - origin).normalized;
        float distToPlayer = Vector3.Distance(origin, player.position);
        float rayLen = Mathf.Min(rayDistance, distToPlayer + 0.05f);

        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, rayLen, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs) Debug.Log($"[LoS] hit={hit.collider.name}, tag={hit.collider.tag}");
            return hit.collider.transform.root.CompareTag("Player");
        }
        return false;
    }

    /// <summary>
    /// 銃口の前方（firePoint.forward）にまっすぐ Ray を撃ち、ヒット処理 / VFX / トレーサー表示
    /// </summary>
    void ShootForward()
    {
        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward; // ★ プレイヤー狙いではなく常に前方
        float dist = rayDistance;

        // 可視化
        Debug.DrawRay(origin, dir * dist, Color.red, 0.1f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Player タグならダメージ
            Transform root = hit.collider.transform.root;
            if (root.CompareTag("Player"))
            {
                var hp = root.GetComponent<Player_HP>() ?? hit.collider.GetComponentInParent<Player_HP>();
                if (hp != null)
                {
                    hp.TakeDamage(damage);
                    if (debugLogs) Debug.Log("[Damage] Player に適用");
                }
                else if (debugLogs) Debug.LogWarning("[Damage] Player_HP が見つかりません");
            }

            // 命中VFX
            if (hitImpactPrefab)
                Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            // トレーサー（命中点まで）
            SpawnTracer(origin, hit.point);
        }
        else
        {
            // トレーサー（外れ：射程端まで）
            SpawnTracer(origin, origin + dir * dist);
        }
    }

    void SpawnTracer(Vector3 from, Vector3 to)
    {
        if (!tracerPrefab) return;
        var tracer = Instantiate(tracerPrefab, from, Quaternion.identity);
        var line = tracer.GetComponent<LineRenderer>();
        if (line)
        {
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }
        Destroy(tracer, 0.1f); // 実運用は Object Pool 推奨
    }

    // デバッグ可視化（検知半径や射程）
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // 検知半径
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectRadius);

        // 射程ライン（現在の前方）
        Transform fp = firePoint ? firePoint : transform;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawLine(fp.position, fp.position + fp.forward * rayDistance);

        // 簡易FOV（水平）
        if (viewHalfAngle > 0f)
        {
            Vector3 origin = transform.position;
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Quaternion.Euler(0f, viewHalfAngle, 0f) * fwd;
            Vector3 left = Quaternion.Euler(0f, -viewHalfAngle, 0f) * fwd;
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
            Gizmos.DrawLine(origin, origin + right * detectRadius);
            Gizmos.DrawLine(origin, origin + left * detectRadius);
        }
    }
}
