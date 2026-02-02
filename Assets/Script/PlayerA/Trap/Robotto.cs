using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto_Move : MonoBehaviour
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

    [Header("攻撃許可（Trigger判定）")]
    [SerializeField] public bool attackAllowed = false;         // Triggerに入ったらtrue
    [SerializeField] private string playerTag = "Player";        // プレイヤーのTag

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

        // 慣性
        float rate = 20f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 走り/待機ブレンド
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);

        // ----------------------------
        // 攻撃条件：Triggerで許可 + 距離で実射程 + クールダウン
        // ----------------------------
        if (attackAllowed && chaseFlag && dist <= attackRange)
        {
            if (!anim.GetBool("Attack") && Time.time - lastAttackTime >= attackCooldown)
            {
                anim.SetBool("Attack", true);
                lastAttackTime = Time.time;
            }
        }

        // 解除：時間ベース（アニメイベントがあるなら保険程度でOK）
        // 例：0.8秒（攻撃クリップ長に合わせて調整）
        if (Time.time - lastAttackTime >= 0.8f)
        {
            anim.SetBool("Attack", false);
        }
    }

    // ----------------------------
    // Triggerで攻撃許可をON/OFF
    // ※プレイヤーにTagを付ける、または root 判定にする
    // ----------------------------
    private void OnTriggerEnter(Collider other)
    {
        // 子コライダー対策：rootでTag判定
        if (other.transform.root.CompareTag(playerTag))
        {
            attackAllowed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root.CompareTag(playerTag))
        {
            attackAllowed = false;

            // 「範囲外に出たら攻撃を強制停止」したいなら有効化
            // anim.SetBool("Attack", false);
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

    private IEnumerator HitStop(float duration)
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

        // 可視化を追加（任意）
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = new Color(1f, 0.2f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}