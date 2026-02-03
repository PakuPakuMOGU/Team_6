/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobottoShoter : MonoBehaviour
{
    
    [Header("参照")]
    public Transform firePoint;
    public Transform player;

    [Header("攻撃許可（他スクリプトのboolを見る）")]
    [Tooltip("参照するやつ")]
    public Robotto_Move gate;   // ★ tatetto への参照（変数名は gate などが分かりやすい）

    [Tooltip("gate が未設定でも撃てるようにする（デバッグ用）。通常は false 推奨")]
    public bool allowShootWhenGateMissing = false;

    [Header("発砲条件")]
    public bool requireLineOfSight = false;
    [Range(0f, 180f)] public float viewHalfAngle = 0f;

    [Header("射撃設定")]
    public float shootInterval = 0.2f;
    public int damage = 10;
    public LayerMask hitMask;

    [Header("VFX（任意）")]
    public GameObject hitImpactPrefab;
    public GameObject tracerPrefab;

    [Header("デバッグ")]
    public bool debugLogs = false;
    public bool drawGizmos = true;

    [Header("レーザー表示（Gameビュー）")]
    public bool showLaserInGame = true;
    public Color laserColor = Color.red;
    public float laserWidth = 0.02f;
    public float laserMaxDistance = 50f;

    LineRenderer _laserLine;

    Coroutine _loop;
    WaitForSeconds _wait;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }

        if (!firePoint) firePoint = transform;

        // gate が未指定なら同じオブジェクトから探す（便利）
        if (!gate) gate = GetComponent<Robotto_Move>();

        _wait = new WaitForSeconds(shootInterval);
    }

    void OnEnable()
    {
        PrepareLaser();
        _loop = StartCoroutine(ShootLoop());
    }


    void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);

        if (_laserLine != null)       // ★追加
            _laserLine.enabled = false;
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

    // ★ p を消して正しく bool から始める
    bool CanShootNow()
    {
        if (!player || !firePoint) return false;

        // ★ 他スクリプトの bool が true の時だけ攻撃可能
        if (gate != null)
        {
            if (!gate.attackAllowed) return false;   // ← tatetto のexternalAttackAllowed を参照
        }
        else
        {
            // gate が設定されてない場合の挙動
            if (!allowShootWhenGateMissing) return false;
        }

        // 視野チェック（任意）
        if (viewHalfAngle > 0f)
        {
            Vector3 toPlayer = player.position - transform.position;

            Vector3 fwd = transform.forward; fwd.y = 0f;
            Vector3 toP = toPlayer; toP.y = 0f;

            if (toP.sqrMagnitude > 0.0001f)
            {
                float ang = Vector3.Angle(fwd.normalized, toP.normalized);
                if (ang > viewHalfAngle) return false;
            }
        }

        // 視線チェック（任意）
        if (requireLineOfSight && !HasLineOfSight()) return false;

        return true;
    }

    bool HasLineOfSight()
    {
        Vector3 origin = firePoint.position;
        Vector3 dirToPlayer = (player.position - origin).normalized;

        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, Mathf.Infinity, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs) Debug.Log($"[LoS] hit={hit.collider.name}, tag={hit.collider.tag}");
            return hit.collider.transform.root.CompareTag("Player");
        }
        return false;
    }

    void ShootForward()
    {
        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward;

        Debug.DrawRay(origin, dir * 50f, Color.red, 0.1f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, Mathf.Infinity, hitMask, QueryTriggerInteraction.Ignore))
        {
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

            if (hitImpactPrefab)
                Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            SpawnTracer(origin, hit.point);
        }
        else
        {
            SpawnTracer(origin, origin + dir * 50f);
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
        Destroy(tracer, 0.6f);
    }

    void Update()
    {
        UpdateLaser(); // ★ゲーム中に見えるレーザー更新
    }

    void UpdateLaser()
    {
        if (!showLaserInGame) return;
        if (_laserLine == null || !firePoint) return;

        // ★「externalAttackAllowed true で撃てる時だけ」表示
        bool can = CanShootNow();
        _laserLine.enabled = can;
        if (!can) return;

        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward;

        Vector3 end = origin + dir * laserMaxDistance;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, laserMaxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
        }

        _laserLine.SetPosition(0, origin);
        _laserLine.SetPosition(1, end);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        if (viewHalfAngle > 0f)
        {
            Vector3 origin = transform.position;
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();

            Vector3 right = Quaternion.Euler(0f, viewHalfAngle, 0f) * fwd;
            Vector3 left = Quaternion.Euler(0f, -viewHalfAngle, 0f) * fwd;

            Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
            const float gizmoLen = 3f;
            Gizmos.DrawLine(origin, origin + right * gizmoLen);
            Gizmos.DrawLine(origin, origin + left * gizmoLen);
        }
    }

    void PrepareLaser()
    {
        if (!showLaserInGame) return;
        if (!firePoint) return;

        if (_laserLine == null)
        {
            // tracerPrefabにLineRendererが付いているなら流用できる
            if (tracerPrefab)
            {
                var obj = Instantiate(tracerPrefab, firePoint.position, Quaternion.identity);
                obj.name = "LaserLineRuntime";
                obj.transform.SetParent(firePoint, worldPositionStays: true);

                _laserLine = obj.GetComponent<LineRenderer>();
                if (_laserLine == null)
                {
                    _laserLine = obj.AddComponent<LineRenderer>();
                }
            }
            else
            {
                var obj = new GameObject("LaserLineRuntime");
                obj.transform.SetParent(firePoint, worldPositionStays: false);
                _laserLine = obj.AddComponent<LineRenderer>();
            }

            // 見た目設定
            _laserLine.positionCount = 2;
            _laserLine.startWidth = laserWidth;
            _laserLine.endWidth = laserWidth;

            // マテリアル（確実に見えるUnlit）
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = laserColor;
            _laserLine.material = mat;

            _laserLine.startColor = laserColor;
            _laserLine.endColor = laserColor;

            _laserLine.useWorldSpace = true;
            _laserLine.enabled = false; // 最初はOFF
        }
    }
}
*/