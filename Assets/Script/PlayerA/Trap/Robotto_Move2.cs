using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class Robotto_Move2 : MonoBehaviour
{

    [Header("対象")]
    public Transform player;

    [Header("挙動（回転）")]
    public float rotateSpeed = 8f;
    public bool lockYOnly = true;

    [Header("追跡（移動）")]
    public bool chase = true;
    public float moveSpeed = 3f;
    public float stopDistance = 1.2f;
    public float acceleration = 20f;
    public bool useRigidbody = true;
    public bool keepOnGround = true;

    [Header("攻撃")]
    public bool enableAttack = true;
    public float attackDistance = 3.0f;
    public float attackCooldown = 1.2f;
    public float attackHoldTime = 0.6f;   // Attack(bool) を true に保つ時間
    public float attackLockTime = 0.6f;   // 攻撃中に移動/追跡を止める時間（基本は hold と同じでOK）

    [Header("アニメーション")]
    public Animator animator;
    public string speedFloatName = "Speed";   // float
    public string attackBoolName = "Attack";  // bool
    public float speedDamp = 8f;              // Speed の追従をなめらかに（大きいほどキビキビ）
    public float speedNormalize = 1f;         // 1なら実速度、0〜1にしたいなら moveSpeed 等を入れる

    [Header("ポーリング")]
    [Tooltip("FixedUpdate 毎に形状ベースで“いま重なっているか”を再評価します")]
    public float pollInterval = 0f;

    [Header("フィルタ")]
    [Tooltip("Player 層に絞る。0 の場合は全層から Player タグで絞る")]
    public LayerMask playerLayer = 0;

    public bool isInside { get; private set; }
    private Transform target;

    private Collider _shape;
    private float _nextPollTime;

    private Rigidbody _rb;
    private Vector3 _currentVelocity;

    private float _nextAttackTime;
    private float _attackEndTime;
    private float _attackBoolOffTime;

    private float _animSpeed; // Animator に渡す Speed を平滑化した値

    private static readonly Collider[] _buf = new Collider[16];

    void Awake()
    {
        _shape = GetComponent<Collider>();
        if (_shape == null) { Debug.LogError("[Robotto_Move] Collider が必要です"); enabled = false; return; }

        _rb = GetComponent<Rigidbody>();

        if (!player)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator) Debug.LogWarning("[Robotto_Move] Animator が見つかりません（アニメ制御は無効）");

        if (!_shape.isTrigger)
        {
            Debug.LogWarning("[Robotto_Move] isTrigger=false ですが、形状ポーリングのみで検知します（仕様上OK）");
        }
    }

    void FixedUpdate()
    {
        // 形状で「中にいるか」をポーリング
        if (pollInterval <= 0f || Time.time >= _nextPollTime)
        {
            RecalculateInsideByShape();
            if (pollInterval > 0f) _nextPollTime = Time.time + pollInterval;
        }

        // Attack(bool) を時間で戻す（Bool運用の要）
        if (animator && animator.GetBool(attackBoolName) && Time.time >= _attackBoolOffTime)
        {
            animator.SetBool(attackBoolName, false);
        }

        if (!isInside || target == null)
        {
            // 中にいない＝待機
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.fixedDeltaTime * acceleration);
            ApplyMove(_currentVelocity);
            SetAnimSpeed(0f);
            SetAttack(false);
            return;
        }

        // 攻撃ロック中は移動しない（攻撃を優先）
        bool attackLocked = Time.time < _attackEndTime;
        if (attackLocked)
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.fixedDeltaTime * acceleration);
            ApplyMove(_currentVelocity);
            SetAnimSpeed(0f);
            return;
        }

        // --- 方向（回転） ---
        Vector3 to = target.position - transform.position;
        if (lockYOnly) to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion tRot = Quaternion.LookRotation(to.normalized, Vector3.up);
        Quaternion newRot = Quaternion.Slerp(transform.rotation, tRot, Time.fixedDeltaTime * rotateSpeed);

        if (_rb && useRigidbody) _rb.MoveRotation(newRot);
        else transform.rotation = newRot;

        float dist = to.magnitude;

        // --- 攻撃（距離に入ったら Attack=true） ---
        if (enableAttack && dist <= attackDistance && Time.time >= _nextAttackTime)
        {
            DoAttack();
            SetAnimSpeed(0f);
            return;
        }

        // --- 追跡（移動） ---
        if (!chase)
        {
            SetAnimSpeed(0f);
            return;
        }

        if (dist <= stopDistance)
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.fixedDeltaTime * acceleration);
            ApplyMove(_currentVelocity);
            SetAnimSpeed(_currentVelocity.magnitude);
            return;
        }

        Vector3 desiredVel = transform.forward * moveSpeed;

        _currentVelocity = Vector3.MoveTowards(
            _currentVelocity,
            desiredVel,
            acceleration * Time.fixedDeltaTime
        );

        ApplyMove(_currentVelocity);

        // アニメへ Speed を渡す（実速度）
        SetAnimSpeed(_currentVelocity.magnitude);
    }

    void DoAttack()
    {
        _nextAttackTime = Time.time + attackCooldown;
        _attackEndTime = Time.time + attackLockTime;

        // bool を true にして、attackHoldTime 後に false に戻す
        SetAttack(true);
        _attackBoolOffTime = Time.time + attackHoldTime;
    }

    // Speed をなめらかに変化させて Animator に渡す
    void SetAnimSpeed(float worldSpeed)
    {
        if (!animator) return;

        // 0〜1に正規化したいなら speedNormalize = moveSpeed を入れる
        float targetSpeed = (speedNormalize > 0f) ? (worldSpeed / speedNormalize) : worldSpeed;

        _animSpeed = Mathf.Lerp(_animSpeed, targetSpeed, 1f - Mathf.Exp(-speedDamp * Time.fixedDeltaTime));
        animator.SetFloat(speedFloatName, _animSpeed);
    }

    void SetAttack(bool on)
    {
        if (!animator) return;
        animator.SetBool(attackBoolName, on);
    }

    void ApplyMove(Vector3 velocity)
    {
        Vector3 delta = velocity * Time.fixedDeltaTime;
        if (keepOnGround) delta.y = 0f;

        if (_rb && useRigidbody) _rb.MovePosition(_rb.position + delta);
        else transform.position += delta;
    }

    void RecalculateInsideByShape()
    {
        bool any = false;
        Transform foundRoot = null;

        int hits = OverlapShapeIntoBuffer();
        for (int i = 0; i < hits; i++)
        {
            var c = _buf[i];
            if (!c) continue;

            if (c.transform.root.CompareTag("Player"))
            {
                any = true;
                foundRoot = c.transform.root;
                break;
            }
        }

        if (any)
        {
            if (!isInside)
            {
                isInside = true;
                target = foundRoot != null ? foundRoot : player;
            }
            else if (target == null)
            {
                target = foundRoot != null ? foundRoot : player;
            }
        }
        else
        {
            if (isInside)
            {
                isInside = false;
                target = null;
                _currentVelocity = Vector3.zero;
                SetAnimSpeed(0f);
                SetAttack(false);
            }
        }
    }

    int OverlapShapeIntoBuffer()
    {
        int mask = playerLayer == 0 ? ~0 : playerLayer.value;

        if (_shape is SphereCollider sc)
        {
            Vector3 center = sc.transform.TransformPoint(sc.center);
            float radius = sc.radius * MaxAbs(sc.transform.lossyScale);
            return Physics.OverlapSphereNonAlloc(center, radius, _buf, mask, QueryTriggerInteraction.Collide);
        }
        else if (_shape is BoxCollider bc)
        {
            Vector3 center = bc.transform.TransformPoint(bc.center);
            Vector3 half = Vector3.Scale(bc.size * 0.5f, Abs(bc.transform.lossyScale));
            return Physics.OverlapBoxNonAlloc(center, half, _buf, bc.transform.rotation, mask, QueryTriggerInteraction.Collide);
        }
        else if (_shape is CapsuleCollider cc)
        {
            GetCapsuleWorld(cc, out var p0, out var p1, out float r);
            return Physics.OverlapCapsuleNonAlloc(p0, p1, r, _buf, mask, QueryTriggerInteraction.Collide);
        }
        else
        {
            var b = _shape.bounds;
            return Physics.OverlapBoxNonAlloc(b.center, b.extents, _buf, Quaternion.identity, mask, QueryTriggerInteraction.Collide);
        }
    }

    static float MaxAbs(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static void GetCapsuleWorld(CapsuleCollider cc, out Vector3 p0, out Vector3 p1, out float r)
    {
        var t = cc.transform;
        var c = t.TransformPoint(cc.center);
        var s = Abs(t.lossyScale);

        int dir = cc.direction; // 0=X,1=Y,2=Z
        if (dir == 0) { r = cc.radius * Mathf.Max(s.y, s.z); }
        else if (dir == 1) { r = cc.radius * Mathf.Max(s.x, s.z); }
        else { r = cc.radius * Mathf.Max(s.x, s.y); }

        float height = (dir == 0 ? cc.height * s.x : dir == 1 ? cc.height * s.y : cc.height * s.z);
        float line = Mathf.Max(0f, height - 2f * r) * 0.5f;

        Vector3 axis = dir == 0 ? t.right : dir == 1 ? t.up : t.forward;
        p0 = c + axis * line;
        p1 = c - axis * line;
    }
}