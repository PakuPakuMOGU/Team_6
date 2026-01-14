
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform player;

    [Header("検知・距離設定")]
    [SerializeField] private float detectRadius = 8.0f;       // 追跡を始める検知半径
    [SerializeField] private float stopDistance = 3.5f;       // この距離で前進を止める
    [SerializeField] private float attackRange = 2.8f;        // 攻撃開始距離（stopDistanceより小さく）

    [Header("移動・回転（減速なし）")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotateSpeedDegPerSec = 600f;

    [Header("攻撃制御")]
    [SerializeField] private float attackCooldown = 1.0f;     // 連続攻撃間隔
    [SerializeField] private bool stopWhileAttacking = false; // 攻撃中は足を止めるか

    private Animator anim;
    private bool chaseFlag = false;        // ← 距離で更新する
    private float currentSpeed = 0f;
    private float lastAttackTime = -999f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false;
    }

    void Start()
    {
        anim.SetBool("Attack", false);
        currentSpeed = 0f;
    }

    void Update()
    {
        if (!player) return;

        // 水平方向のみで距離と向き
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // --- 追跡フラグを距離で決定 ---
        // プレイヤーが検知半径内にいれば追跡ON、外ならOFF
        bool inDetect = dist <= detectRadius;
        chaseFlag = inDetect;

        // 向きを合わせる
        if (chaseFlag && toPlayer.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
        }

        // --- 速度決定 ---
        float desiredSpeed = 0f;
        if (chaseFlag)
        {
            bool inStopZone = dist <= stopDistance; // 近すぎ防止
            bool canMove = !inStopZone;

            if (stopWhileAttacking)
            {
                // 攻撃中は足を止めたい場合
                canMove = canMove && !anim.GetBool("Attack");
            }

            desiredSpeed = canMove ? moveSpeed : 0f;
        }

        // 立ち上がりを速めたスムージング（慣性）
        float rate = 20f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動（前進）
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Animator（走り/待機ブレンド）
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);

        // --- 攻撃条件：攻撃距離内 + クールダウン ---
        if (chaseFlag && dist <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                anim.ResetTrigger("Attack");   // 念のため
                anim.SetTrigger("Attack");
                anim.SetBool("Attack", true);  // 足を止める/ブレンドに使うならON
                lastAttackTime = Time.time;
            }
        }

        // 時間ベースの簡易解除（本番はアニメイベント推奨）
        if (Time.time - lastAttackTime >= 0.2f)
        {
            anim.SetBool("Attack", false);
        }
    }

    // もうトリガーには依存しません（削除OK）
    void OnTriggerEnter(Collider other) { }
    void OnTriggerExit(Collider other) { }

    // アニメーションイベント（ヒット）
    public void AE_AttackHit()
    {
        // 攻撃・ダメージは別スクリプトでOK
        // ここでは追跡側の状態を変えない（止めたい場合は HitStop で時間付き停止）
    }

    // アニメーションイベント（終わり）
    public void AE_AttackEnd()
    {
        // 攻撃終了で必ず解除（安全策）
        anim.SetBool("Attack", false);
    }

    // （任意）短いヒットストップを入れたい場合
    public void DoHitStop(float duration)
    {
        StartCoroutine(HitStop(duration));
    }

    private System.Collections.IEnumerator HitStop(float duration)
    {
        bool prevAttack = anim.GetBool("Attack");
        anim.SetBool("Attack", true);  // 足を止めたい時
        yield return new WaitForSeconds(duration);
        anim.SetBool("Attack", prevAttack);   // 攻撃中でなければ false に戻る
    }

    // Gizmo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
