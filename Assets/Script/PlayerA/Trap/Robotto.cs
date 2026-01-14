
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform player;

    [Header("検知・距離")]
    [SerializeField] private float stoppingDistance = 3.0f;   // ここで停止
    [SerializeField] private float slowDownRange = 2.5f;      // 手前から減速開始（推奨：stoppingDistanceより大きい）
    [SerializeField] private float triggerRadiusGizmo = 6.0f; // Gizmo表示用（実際の半径はSphereCollider側）

    [Header("移動・回転（速度ベース）")]
    [SerializeField] private float maxMoveSpeed = 3.5f;       // 遠距離ではこの速度で近づく
    [SerializeField] private float accel = 8.0f;              // 加速度（m/s^2）
    [SerializeField] private float decel = 12.0f;             // 減速度（m/s^2）
    [SerializeField] private float rotateSpeedDegPerSec = 600f; // 回頭速度（度/秒）

    private Animator anim;
    private bool chaseFlag = false;
    private float currentSpeed = 0f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false; // 見た目のみ
        
    }

    void Start()
    {
        anim.SetBool("Attack", false);
    }

    void Update()
    {
        if (player == null) return;

        // 方向（水平のみ）
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (chaseFlag)
        {
            // 向きを合わせる（水平回転）
            if (toPlayer.sqrMagnitude > 1e-6f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, rotateSpeedDegPerSec * Time.deltaTime);
            }
        }

        // --- 速度決定（遠距離で止まらないように分岐を追加） ---
        float desiredSpeed = 0f;

        if (chaseFlag)
        {
            float farBand = stoppingDistance + slowDownRange;

            if (dist > farBand)
            {
                // 減速帯よりも遠い → 最大速度で接近
                desiredSpeed = maxMoveSpeed;
            }
            else if (dist > stoppingDistance)
            {
                // 減速帯に入った → 距離に応じて0〜maxMoveSpeedへ補間
                float t = Mathf.InverseLerp(farBand, stoppingDistance, dist); // 0..1
                desiredSpeed = Mathf.Lerp(0f, maxMoveSpeed, t);
            }
            else
            {
                // 停止距離内 → 停止
                desiredSpeed = 0f;
            }
        }

        // 加減速で滑らかに
        float rate = (desiredSpeed > currentSpeed) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Animator（速度ベース：Idle↔Run）
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    // トリガー：半径内に入ったら追跡開始／出たら停止
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaseFlag = true;
            Debug.Log("追跡開始（トリガーEnter）");
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaseFlag = false;
            Debug.Log("追跡停止（トリガーExit）");
        }
    }

    // 検知範囲のGizmo（目安）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, triggerRadiusGizmo);
    }
}
