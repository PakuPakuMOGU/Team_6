using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Robotto_Move : MonoBehaviour
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

    [Header("ポーリング")]
    [Tooltip("FixedUpdate 毎に形状ベースで“いま重なっているか”を再評価します")]
    public float pollInterval = 0f; // 0 = 毎FixedUpdate

    [Header("フィルタ")]
    [Tooltip("Player 層に絞る。0 の場合は全層から Player タグで絞る")]
    public LayerMask playerLayer = 0;

    public bool isInside { get; private set; }
    private Transform target;

    private Collider _shape;
    private float _nextPollTime;

    private Rigidbody _rb;
    private Vector3 _currentVelocity;

    private static readonly Collider[] _buf = new Collider[16];

    void Awake()
    {
        _shape = GetComponent<Collider>();
        if (_shape == null) { Debug.LogError("[taretto] Collider が必要です"); enabled = false; return; }

        // Rigidbody任意
        _rb = GetComponent<Rigidbody>();

        // 参照がない場合はタグから拾う
        if (!player) { var p = GameObject.FindWithTag("Player"); if (p) player = p.transform; }

        if (!_shape.isTrigger)
        {
            Debug.LogWarning("[taretto] isTrigger=false ですが、形状ポーリングのみで検知します（仕様上OK）");
        }

        // Rigidbodyがある場合、追跡用なら回転/移動を物理側で扱いやすくする
        if (_rb && useRigidbody)
        {
            // 物理挙動を使うなら基本はKinematic推奨（押されたり吹っ飛んだりしたくない場合）
            // ただし押し合いしたいなら isKinematic=false のままでもOKです
            // _rb.isKinematic = true;
        }
    }

    void FixedUpdate()
    {
        if (pollInterval <= 0f || Time.time >= _nextPollTime)
        {
            RecalculateInsideByShape();
            if (pollInterval > 0f) _nextPollTime = Time.time + pollInterval;
        }

        if (!isInside || target == null) return;

        // --- 方向（回転） ---
        Vector3 to = target.position - transform.position;
        if (lockYOnly) to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion tRot = Quaternion.LookRotation(to.normalized, Vector3.up);
        Quaternion newRot = Quaternion.Slerp(transform.rotation, tRot, Time.fixedDeltaTime * rotateSpeed);

        if (_rb && useRigidbody)
            _rb.MoveRotation(newRot);
        else
            transform.rotation = newRot;

        // --- 追跡（移動） ---
        if (!chase) return;

        // 距離が近いなら止まる
        float dist = to.magnitude;
        if (dist <= stopDistance)
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, Time.fixedDeltaTime * acceleration);
            ApplyMove(_currentVelocity);
            return;
        }

        // 前方に進む（回転後の forward を使うと「向いた方向へ追う」挙動になる）
        Vector3 desiredVel = transform.forward * moveSpeed;

        // 急に速度が変わらないように加速でならす
        _currentVelocity = Vector3.MoveTowards(
            _currentVelocity,
            desiredVel,
            acceleration * Time.fixedDeltaTime
        );

        ApplyMove(_currentVelocity);
    }

    void ApplyMove(Vector3 velocity)
    {
        Vector3 delta = velocity * Time.fixedDeltaTime;

        if (keepOnGround)
        {
            // Y方向を変えたくない（段差対応するなら別途接地処理が必要）
            delta.y = 0f;
        }

        if (_rb && useRigidbody)
        {
            _rb.MovePosition(_rb.position + delta);
        }

        else
        {
            transform.position += delta;
        }
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