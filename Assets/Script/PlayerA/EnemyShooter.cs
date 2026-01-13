
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class EnemyShooter : MonoBehaviour
{
    [Header("ターゲット")]
    public Transform player;
    public string playerTag = "Player";

    [Header("移動（NavMeshなし）")]
    public float moveSpeed = 6f;        // 最大速度（走り）
    public float walkSpeed = 2.2f;      // 参考用（Blend Treeの調整に）
    public float acceleration = 18f;    // 加速・減速
    public float turnSpeed = 360f;      // 回頭速度（度/秒）
    public float chaseRange = 15f;      // 追いかけ始める距離
    public float keepDistance = 7f;     // 近づきすぎない距離
    public float disengageRange = 25f;  // 追跡をやめる距離

    [Header("射撃（Raycast/ヒットスキャン）")]
    public Transform muzzle;            // 銃口
    public float fireCooldown = 0.35f;  // 連射間隔
    public float fireRange = 50f;       // 射程
    public float aimOffsetHeight = 1.2f;// 胸〜頭の高さ狙い
    public bool requireLineOfSight = true;
    public float spreadDegrees = 1.2f;
    public LayerMask hitMask = ~0;

    [Header("エフェクト・SE")]
    public GameObject impactEffectPrefab;  // ヒットパーティクル
    public LineRenderer tracerPrefab;      // 弾道線
    public float tracerDuration = 0.06f;
    public AudioSource audioSource;
    public AudioClip fireSFX;

    [Header("ダメージ")]
    public float damage = 15f;

    [Header("アニメーション連動")]
    public int upperBodyLayerIndex = 1;      // 上半身レイヤー（Attack用）
    public float aimLayerBlendSpeed = 6f;    // 上半身レイヤーのウェイト補間

    private Rigidbody rb;
    private Animator animator;
    private float nextFireTime = 0f;

    // 内部：現在Aimingかどうか（アニメ層のブレンドに使用）
    private bool isAiming;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // 横転防止（物理移動＋アニメの安定化）
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }

        // 物理移動を使うのでルートモーションは基本OFF推奨
        animator.applyRootMotion = false;
    }

    void FixedUpdate()
    {

        animator.applyRootMotion = false;

        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 1) 回頭（水平のみ）
        FacePlayerYawOnly();

        // 2) 追従（近づき過ぎない）
        Vector3 desiredVel = Vector3.zero;
        if (distance <= chaseRange && distance > keepDistance)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            desiredVel = toPlayer.normalized * moveSpeed;
        }
        if (distance > disengageRange)
        {
            desiredVel = Vector3.zero;
        }

        Vector3 v = rb.velocity;
        v = Vector3.MoveTowards(v, desiredVel, acceleration * Time.fixedDeltaTime);
        v.y = rb.velocity.y; // 重力はそのまま
        rb.velocity = v;

        // 3) アニメーション更新（歩き/走り）
        UpdateLocomotionAnimation();

        // 4) 射撃（Raycast）
        HandleAimingAndFire(distance);
    }

    void FacePlayerYawOnly()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    void UpdateLocomotionAnimation()
    {
        // 水平速度の大きさをSpeedとしてAnimatorへ
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float speedMag = horizontalVel.magnitude;

        // Blend Tree用：SpeedでIdle→Walk→Runをブレンド
        animator.SetFloat("Speed", speedMag);

        // 必要なら「IsMoving」を補助で設定（状態遷移などに使う）
        bool isMoving = speedMag > 0.1f;
        animator.SetBool("IsMoving", isMoving);
    }

    void HandleAimingAndFire(float distance)
    {
        // 視線チェック（撃つ条件）
        bool canAim = distance <= fireRange && (!requireLineOfSight || HasLineOfSight());

        // Aimingフラグ（上半身レイヤーをブレンド）
        isAiming = canAim;
        animator.SetBool("IsAiming", isAiming);

        // 上半身レイヤーのウェイトをスムーズに
        if (upperBodyLayerIndex >= 0 && upperBodyLayerIndex < animator.layerCount)
        {
            float cur = animator.GetLayerWeight(upperBodyLayerIndex);
            float target = isAiming ? 1f : 0f;
            float next = Mathf.MoveTowards(cur, target, aimLayerBlendSpeed * Time.deltaTime);
            animator.SetLayerWeight(upperBodyLayerIndex, next);
        }

        // クールダウン
        if (!canAim || Time.time < nextFireTime) return;

        // 攻撃開始：アニメの「Attack」トリガーで再生
        animator.SetTrigger("Attack");

        // 実際の発射はアニメーションイベントで呼ぶ想定（同期が綺麗）
        // Attackモーション内の適切なフレームに "AnimEvent_Fire" を設定してください。

        nextFireTime = Time.time + fireCooldown;
    }

    // ==== アニメーションイベントから呼ぶ（モーションの適切なタイミングで） ====
    // AnimatorのAttackモーションに Animation Event を追加し、このメソッド名を設定する。
    public void AnimEvent_Fire()
    {
        if (muzzle == null || player == null) return;

        Vector3 origin = muzzle.position;
        Vector3 target = player.position + Vector3.up * aimOffsetHeight;
        Vector3 dir = (target - origin).normalized;
        dir = ApplySpread(dir, spreadDegrees);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, fireRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            // ダメージ（IHealthを仮定）
            var health = hit.collider.GetComponentInParent<IHealth>();
            if (health != null) health.TakeDamage(damage);

            // ヒットエフェクト
            if (impactEffectPrefab)
            {
                var fx = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, 3f);
            }

            // トレーサー線
            SpawnTracer(origin, hit.point);
        }
        else
        {
            Vector3 endPoint = origin + dir * fireRange;
            SpawnTracer(origin, endPoint);
        }

        // 発射SE
        if (audioSource && fireSFX) audioSource.PlayOneShot(fireSFX);
    }

    bool HasLineOfSight()
    {
        Vector3 origin = muzzle ? muzzle.position : (transform.position + Vector3.up * 1.2f);
        Vector3 target = player.position + Vector3.up * aimOffsetHeight;
        Vector3 dir = (target - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, fireRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player)) return true;
            return false;
        }
        return true; // 何も当たらなければ通っている扱い
    }

    Vector3 ApplySpread(Vector3 forward, float degrees)
    {
        if (degrees <= 0f) return forward;
        // 前方向に対するコーン内ランダム
        // より素直な揺らぎ（Yaw/Pitchにランダム角度を加算）
        float yaw = Random.Range(-degrees, degrees);
        float pitch = Random.Range(-degrees, degrees);
        Quaternion q = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
        return (q * forward).normalized;
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (tracerPrefab == null) return;
        LineRenderer lr = Instantiate(tracerPrefab);
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        Destroy(lr.gameObject, tracerDuration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, keepDistance);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, disengageRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, fireRange);
    }
}

// ダメージ受け側のインターフェース例
public interface IHealth
{
    void TakeDamage(float amount);
}
