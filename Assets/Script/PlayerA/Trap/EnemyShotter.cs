
using UnityEngine;

public class EnemyStraightRayShooter : MonoBehaviour
{
    [Header("参照")]
    public Transform firePoint;        // 銃口（Z+が前）

    [Header("射撃設定")]
    public float shootInterval = 0.2f; // レーザーを撃つ間隔（連射）
    public float rayDistance = 30f;    // レーザーの長さ
    public int damage = 10;
    public LayerMask hitMask;          // Player + 遮蔽物（Enemyは外す）

    [Header("VFX（任意）")]
    public GameObject hitImpactPrefab; // 命中エフェクト
    public GameObject tracerPrefab;    // LineRenderer 等（短寿命）

    float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= shootInterval)
        {
            _timer = 0f;
            ShootForward();
        }
    }

    void ShootForward()
    {
        if (!firePoint) return;

        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward; // ★ “常に前方”に撃つ
        float dist = rayDistance;

        Debug.DrawRay(origin, dir * dist, Color.red, 0.1f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Playerに当たったらダメージ
            Transform root = hit.collider.transform.root;
            if (root.CompareTag("Player"))
            {
                var hp = root.GetComponent<Player_HP>() ?? hit.collider.GetComponentInParent<Player_HP>();
                if (hp != null) hp.TakeDamage(damage);
            }

            // VFX
            if (hitImpactPrefab)
                Instantiate(hitImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            SpawnTracer(origin, hit.point); // 命中点まで
        }
        else
        {
            SpawnTracer(origin, origin + dir * dist); // 外れ：射程端まで
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
        Destroy(tracer, 0.1f); // 短寿命
    }
}
