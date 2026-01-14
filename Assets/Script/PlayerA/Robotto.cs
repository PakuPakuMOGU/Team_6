
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Robotto : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform player;

    [Header("移動・回転")]
    [SerializeField] private float maxMoveSpeed = 3.5f;   // 最高速度（m/s）: Runの見た目に合わせる
    [SerializeField] private float accel = 8.0f;          // 加速度
    [SerializeField] private float decel = 12.0f;         // 減速度
    [SerializeField] private float rotateSpeedDegPerSec = 360f;

    [Header("距離設定")]
    [SerializeField] private float stoppingDistance = 3.0f;
    [SerializeField] private float slowDownRange = 2.0f;  // この範囲で減速開始

    private Animator anim;
    private bool chaseFlag = false;
    private float currentSpeed = 0f; // m/s（移動＆アニメ共有）

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim) anim.applyRootMotion = false; // 移動はスクリプトで行う
    }

    void Update()
    {
        float desiredSpeed = 0f;

        if (chaseFlag && player != null)
        {
            // 水平面の向き
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            // 回転（水平のみ）
            if (toPlayer.sqrMagnitude > 1e-6f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotateSpeedDegPerSec * Time.deltaTime
                );
            }

            float dist = toPlayer.magnitude;
            if (dist > stoppingDistance)
            {
                // 距離に応じた減速（速度ベース）
                float t = Mathf.InverseLerp(stoppingDistance + slowDownRange, stoppingDistance, dist);
                desiredSpeed = Mathf.Lerp(0f, maxMoveSpeed, t);
            }
        }

        // 加減速で currentSpeed を目標に寄せる
        float rate = (desiredSpeed > currentSpeed) ? accel : decel;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);

        // 実移動
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Animatorへ速度（m/s）をそのまま渡す（Blend Tree が Idle/Run をブレンド）
        // DampTime=0.1s で滑らかに追従
        anim.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    // トリガーで追跡開始／停止
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaseFlag = true;
            // 追跡開始直後にガタつく場合は currentSpeed を少し上げて開始しても可
            // currentSpeed = Mathf.Max(currentSpeed, 0.5f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaseFlag = false; // Update 内で decel により 0 へ
        }
    }
}
