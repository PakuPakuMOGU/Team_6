
using UnityEngine;
using System.Collections.Generic;

public class ParentLookAtController : MonoBehaviour
{
    [Header("プレイヤー検出")]
    public Transform player; // 空ならタグで自動取得

    [Header("回転設定")]
    public float rotateSpeedDegPerSec = 180f; // 0なら瞬時
    public bool lockYRotation = true;         // 水平面のみ回す

    // 子トリガーからの滞在管理（複数Collider対策）
    private readonly HashSet<Collider> _insideColliders = new HashSet<Collider>();
    private bool IsTracking => _insideColliders.Count > 0;

    // 物理や他コンポーネント対策（任意）
    public bool usePhysicsRotation = false; // Rigidbodyがある場合はMoveRotationで
    private Rigidbody _rb;
    private UnityEngine.AI.NavMeshAgent _agent;
    private Animator _anim;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _anim = GetComponent<Animator>();

        // 競合回避（必要に応じて有効化）
        if (_agent != null) _agent.updateRotation = false; // Agentに回転させない
        // if (_anim != null) _anim.applyRootMotion = false; // RootMotionで回転しない場合
    }

    private void LateUpdate()
    {
        if (!IsTracking || player == null) return;

        Vector3 targetPos = player.position;
        if (lockYRotation) targetPos.y = transform.position.y;

        Vector3 dir = (targetPos - transform.position);
        if (dir.sqrMagnitude <= 0f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (rotateSpeedDegPerSec <= 0f)
        {
            ApplyRotation(targetRot);
        }
        else
        {
            Quaternion next = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime
            );
            ApplyRotation(next);
        }
    }

    private void ApplyRotation(Quaternion rot)
    {
        if (usePhysicsRotation && _rb != null)
        {
            // 物理で回す（Freeze Rotationの影響を受けない設定に）
            _rb.MoveRotation(rot);
        }
        else
        {
            transform.rotation = rot;
        }
    }

    // ---- 子トリガーからの通知（Collider別に管理） ----
    public void NotifyEnter(Collider c)
    {
        if (c != null) _insideColliders.Add(c);
    }

    public void NotifyExit(Collider c)
    {
        if (c != null) _insideColliders.Remove(c);
    }
}
