
using UnityEngine;
using System.Collections;

public class PlayerAttackAdvanced : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField, Tooltip("アニメイベントを使わない場合の攻撃モーション長（秒）")]
    private float attackDuration = 0.6f;

    [SerializeField, Tooltip("攻撃後のクールダウン時間（秒）")]
    private float cooldown = 0.8f;

    [Header("Animator (任意)")]
    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("攻撃用トリガー名")]
    private string attackTriggerName = "Attack";

    [Header("Hitbox (任意)")]
    [SerializeField, Tooltip("攻撃中に有効化するヒットボックス（Colliderや任意のスクリプト）")]
    private Behaviour[] hitboxBehaviours;
    [SerializeField] private Collider[] hitboxColliders;

    [Header("Input (旧Input用)")]
    [SerializeField] private KeyCode attackKey = KeyCode.E;

    [Header("Options")]
    [SerializeField, Tooltip("攻撃中に回避/ダッシュ等でキャンセル可能にするか")]
    private bool allowCancel = true;

    // 状態フラグ
    public bool Attack { get; private set; } = false;  // 攻撃〜CDの包括状態（CD中もtrue）
    public bool canAttack { get; private set; } = true; // 次の攻撃入力受付
    public bool isAttacking { get; private set; } = false; // 見た目のモーション中

    private Coroutine attackFlowRoutine;
    private bool useAnimationEventTiming = false; // アニメイベントで閉じる場合は true になる

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            TryAttack();
        }

        // 例：キャンセル入力（ここでは仮に左Shift）
        if (allowCancel && isAttacking && Input.GetKeyDown(KeyCode.LeftShift))
        {
            CancelAttack(imposeShortCooldown: true);
        }
    }

    public void TryAttack()
    {
        if (!canAttack) return;
        if (attackFlowRoutine != null) StopCoroutine(attackFlowRoutine);
        attackFlowRoutine = StartCoroutine(AttackFlow());
    }

    private IEnumerator AttackFlow()
    {
        canAttack = false;
        Attack = true;

        // 見た目の攻撃開始
        isAttacking = true;
        SetHitboxActive(true);
        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.ResetTrigger(attackTriggerName);
            animator.SetTrigger(attackTriggerName);
        }

        // ① アニメイベントを使わない場合：攻撃時間は WaitForSeconds
        // ② アニメイベントを使う場合：OnAttackAnimationEnd() が呼ばれるまで待つ
        if (!useAnimationEventTiming)
        {
            yield return new WaitForSeconds(attackDuration);
            OnAttackAnimationEnd(); // 手動で終端処理
        }
        else
        {
            // アニメイベントから isAttacking=false に落ちるのを待つ
            while (isAttacking)
                yield return null;
        }

        // クールダウン（CD中も Attack は true）
        yield return new WaitForSeconds(cooldown);

        // 次の攻撃が可能になった瞬間にだけ false
        Attack = false;
        canAttack = true;

        attackFlowRoutine = null;
    }

    /// <summary>
    /// アニメーションイベントから呼ぶ用。
    /// 攻撃モーションの終端で呼び、ヒットボックスを閉じる。
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        if (!isAttacking) return; // 多重呼び出しガード
        isAttacking = false;
        SetHitboxActive(false);
    }

    /// <summary>
    /// 途中キャンセル（回避・被弾等）。Attack は CDの扱いに従って後で false。
    /// </summary>
    public void CancelAttack(bool imposeShortCooldown = true)
    {
        if (!isAttacking) return;

        // 進行中の攻撃フローはここでは止めない（CD処理に流す）
        isAttacking = false;
        SetHitboxActive(false);

        if (animator)
        {
            // 必要ならキャンセル用ステートへ遷移など
            // animator.SetTrigger("Cancel");
        }

        // 任意：キャンセル時はクールダウン短縮
        if (imposeShortCooldown)
        {
            // 既存のフローを置き換え、短CDで復帰
            if (attackFlowRoutine != null) StopCoroutine(attackFlowRoutine);
            attackFlowRoutine = StartCoroutine(CancelRecoveryShortCD(0.25f));
        }
    }

    private IEnumerator CancelRecoveryShortCD(float shortCd)
    {
        // Attack は既に true のまま（包括状態維持）
        yield return new WaitForSeconds(shortCd);
        Attack = false;
        canAttack = true;
        attackFlowRoutine = null;
    }

    private void SetHitboxActive(bool active)
    {
        if (hitboxBehaviours != null)
        {
            foreach (var b in hitboxBehaviours)
                if (b) b.enabled = active;
        }
        if (hitboxColliders != null)
        {
            foreach (var c in hitboxColliders)
                if (c) c.enabled = active;
        }
    }

    // --- アニメイベント運用を使いたい場合は、Inspectorから true にするメソッドを追加 ---
    [ContextMenu("Use Animation Event Timing")]
    private void EnableAnimationEventTiming()
    {
        useAnimationEventTiming = true;
    }
}
