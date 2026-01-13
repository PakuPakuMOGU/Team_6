
using UnityEngine;
using UnityEngine.AI;

public class LookAtByDistance : MonoBehaviour
{
    [Header("プレイヤー参照（空ならタグ Player を検索）")]
    public Transform player;

    [Header("有効距離（この距離以内なら常に向く）")]
    public float activeRadius = 5f;

    [Header("回転設定")]
    public float rotateSpeedDegPerSec = 180f; // 0で瞬時回転
    public bool lockYRotation = true;         // 水平面のみ回転

    // 競合対策
    private NavMeshAgent _agent;
    private Animator _anim;
    private Rigidbody _rb;

    private void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();

        // NavMeshAgentが回転を上書きしないように
        if (_agent != null) _agent.updateRotation = false;
        // Root Motionが回転を含むなら、必要に応じて無効化（任意）
        // if (_anim != null) _anim.applyRootMotion = false;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > activeRadius) return; // 距離外なら何もしない

        Vector3 targetPos = player.position;
        if (lockYRotation) targetPos.y = transform.position.y;

        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude <= 1e-6f) return;

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
        // Rigidbodyがあるなら、必要に応じて物理回転へ
        if (_rb != null && !_rb.isKinematic)
        {
            _rb.MoveRotation(rot); // 物理挙動に従う
        }
        else
        {
            transform.rotation = rot; // 通常のTransform回転
        }
    }

    // シーンビューで半径を可視化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activeRadius);
    }
}
