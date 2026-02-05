using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto_Move : MonoBehaviour
{
    [Header("参照（未設定ならPlayerタグで自動検知）")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float findInterval = 0.5f; // 探索間隔

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

    private Animator anim;
    private bool chaseFlag = false;
    public bool isInside = false;

    private float currentSpeed = 0f;
    private float lastAttackTime = -999f;

    private float nextFindTime = 0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false;
    }

    void Start()
    {
        // Animator 側に "Attack"(Bool) と "Speed"(Float) がある前提
        anim.SetBool("Attack", false);
        currentSpeed = 0f;

        if (attackRange >= stopDistance)
            Debug.LogWarning("[Robotto] attackRange は stopDistance より小さくしてください。");
    }

    void Update()
    {
        // Playerがまだ生成されていない/見つかってない場合は探す
        if (!player)
        {
            TryFindPlayerByTag();
            return;
        }

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

        // 慣性
        float rate = 20f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 走り/待機ブレンド
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);

        // 攻撃条件：距離内 + クールダウン
        if (chaseFlag && dist <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                isInside = true;
                anim.SetBool("Attack", true);
                lastAttackTime = Time.time;
            }
        }
        else
        {
            // 攻撃範囲外に出たらフラグを戻したいなら
            isInside = false;
        }

        // 解除：時間ベース（クリップ長に合わせて延長）
        if (Time.time - lastAttackTime >= 0.8f)
        {
            anim.SetBool("Attack", false);
        }
    }

    private void TryFindPlayerByTag()
    {
        if (Time.time < nextFindTime) return;
        nextFindTime = Time.time + findInterval;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p)
        {
            // タグが子に付いていてもズレにくいようroot推奨
            player = p.transform.root;
            Debug.Log("[Robotto] Playerをタグ検索で取得しました: " + player.name);
        }
    }

    // アニメーションイベント（ヒット）
    public void AE_AttackHit()
    {
        // ダメージ処理等は別スクリプトへ
    }

    // アニメーションイベント（終わり）
    public void AE_AttackEnd()
    {
        anim.SetBool("Attack", false);
    }

    // （任意）ヒットストップ
    public void DoHitStop(float duration)
    {
        StartCoroutine(HitStop(duration));
    }

    private System.Collections.IEnumerator HitStop(float duration)
    {
        bool prevAttack = anim.GetBool("Attack");
        anim.SetBool("Attack", true);
        yield return new WaitForSeconds(duration);
        anim.SetBool("Attack", prevAttack);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = new Color(1f, 0.2f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}