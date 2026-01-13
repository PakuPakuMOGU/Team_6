
using UnityEngine;
using System;

public class Health : MonoBehaviour, IHealth
{
    [Header("しきい値")]
    [Tooltip("累積ダメージがこの値に達すると死亡")]
    public float damageToDie = 100f;

    [Header("アニメ")]
    public Animator animator;
    [Tooltip("被弾時トリガー名")]
    public string hitTriggerName = "Hit";
    [Tooltip("死亡時トリガー名")]
    public string dieTriggerName = "Die";
    [Tooltip("死亡フラグ（レイヤーブレンドや遷移に使う場合）")]
    public string isDeadBoolName = "IsDead";

    [Header("ラグドール（任意）")]
    public bool useRagdollOnDeath = false;
    public Rigidbody[] ragdollBodies;
    public Collider[] ragdollColliders;
    public Rigidbody mainRigidbody; // 移動用Rigidbody（敵の場合）
    public Behaviour[] componentsToDisableOnDeath; // AIスクリプトなど

    [Header("後処理")]
    [Tooltip("死亡後、何秒で自動削除するか。0以下なら削除しない")]
    public float autoDestroyAfterSeconds = 8f;

    public float AccumulatedDamage { get; private set; } = 0f;
    public bool IsDead { get; private set; } = false;

    public event Action<float, float> OnDamaged; // (今回ダメージ, 累積)
    public event Action OnDied;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // ラグドール初期化（通常は無効化＝isKinematic ON）
        SetRagdollActive(false);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        AccumulatedDamage += Mathf.Max(0f, amount);
        OnDamaged?.Invoke(amount, AccumulatedDamage);

        // 被弾リアクション（任意）
        if (animator && !string.IsNullOrEmpty(hitTriggerName))
        {
            animator.SetTrigger(hitTriggerName);
        }

        if (AccumulatedDamage >= damageToDie)
        {
            Die();
        }
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 死亡アニメ
        if (animator)
        {
            if (!string.IsNullOrEmpty(dieTriggerName)) animator.SetTrigger(dieTriggerName);
            if (!string.IsNullOrEmpty(isDeadBoolName)) animator.SetBool(isDeadBoolName, true);
        }

        // 物理移動停止
        if (mainRigidbody)
        {
            mainRigidbody.velocity = Vector3.zero;
            mainRigidbody.angularVelocity = Vector3.zero;
        }

        // AIや移動系を停止
        if (componentsToDisableOnDeath != null)
        {
            foreach (var c in componentsToDisableOnDeath)
            {
                if (c) c.enabled = false;
            }
        }

        // ラグドールへ切替（任意）
        if (useRagdollOnDeath)
        {
            // アニメ停止→ラグドールON
            if (animator) animator.enabled = false;
            SetRagdollActive(true);
        }

        OnDied?.Invoke();

        // 自動削除
        if (autoDestroyAfterSeconds > 0f)
        {
            Destroy(gameObject, autoDestroyAfterSeconds);
        }
    }

    void SetRagdollActive(bool active)
    {
        if (ragdollBodies != null)
        {
            foreach (var rb in ragdollBodies)
            {
                if (!rb) continue;
                rb.isKinematic = !active;
                rb.detectCollisions = active;
            }
        }
        if (ragdollColliders != null)
        {
            foreach (var col in ragdollColliders)
            {
                if (!col) continue;
                col.enabled = active;
            }
        }
    }
}

