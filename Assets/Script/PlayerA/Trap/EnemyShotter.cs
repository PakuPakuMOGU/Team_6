using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform player;

    [Header("検知・距離設定")]
    [SerializeField] private float detectRadius = 8.0f;
    [SerializeField] private float stopDistance = 3.5f;   // 攻撃距離より大きく
    [SerializeField] private float attackRange = 2.8f;    // stopDistance より小さく

    [Header("移動・回転（減速なし）")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotateSpeedDegPerSec = 600f;

    [Header("攻撃制御")]
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private bool stopWhileAttacking = false;

    [Header("外部制御（他スクリプトから攻撃を許可/禁止）")]
    [SerializeField] private bool externalAttackAllowed = true;

    /// <summary>
    /// 外部（他スクリプト）から攻撃可否をON/OFFできるスイッチ
    /// </summary>
    public bool ExternalAttackAllowed
    {
        get => externalAttackAllowed;
        set => externalAttackAllowed = value;
    }

    /// <summary>
    /// 内部条件（距離・クールダウン等）＋外部許可を含めた最終的な攻撃可否
    /// 他スクリプトから参照用（読み取り専用）
    /// </summary>
    public bool CanAttack { get; private set; }

    /// <summary>
    /// 現在攻撃モーション中か（AnimatorのAttack Bool）
    /// </summary>
    public bool IsAttacking => anim != null && anim.GetBool("Attack");

    private Animator anim;
    private bool chaseFlag = false;
    private float currentSpeed = 0f;
    private float lastAttackTime = -999f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false;
    }

    void Start()
    {
        if (!player)
            Debug.LogWarning("[Robotto] player が未割り当てです。インスペクタで設定してください。");

        // Animator 側に "Attack"(Bool) と "Speed"(Float) がある前提
        anim.SetBool("Attack", false);
        currentSpeed = 0f;

        // 値の整合性チェック（必要なら OnValidate に移行）
        if (attackRange >= stopDistance)
            Debug.LogWarning("[Robotto] attackRange は stopDistance より小さくしてください。");
    }

    void Update()
    {
        if (!player) return;

        // 水平方向のみで距離と向き
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // 追跡フラグ
        chaseFlag = (dist <= detectRadius);

        // 向きを合わせる
        if (chaseFlag && toPlayer.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
        }

        // 速度決定
        float desiredSpeed = 0f;
        if (chaseFlag)
        {
            bool inStopZone = dist <= stopDistance;
            bool canMove = !inStopZone;

            if (stopWhileAttacking)
            {
                // 攻撃中は止める設定
                canMove = canMove && !anim.GetBool("Attack");
            }

            desiredSpeed = canMove ? moveSpeed : 0f;
        }

        // 慣性（※元コードのまま：厳密には慣性というより追従の滑らか化）
        float rate = 20f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 走り/待機ブレンド
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);

        // --- 攻撃判定（外部許可を含む） ---
        bool inAttackRange = chaseFlag && dist <= attackRange;
        bool cooldownOK = (Time.time - lastAttackTime) >= attackCooldown;

        // 他スクリプト参照用：最終的な攻撃可否
        CanAttack = externalAttackAllowed && inAttackRange && cooldownOK;

        // 攻撃開始
        if (CanAttack)
        {
            anim.SetBool("Attack", true); // Bool 運用
            lastAttackTime = Time.time;
        }

    }
}
