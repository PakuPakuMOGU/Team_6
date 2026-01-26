
using UnityEngine;

public class tarerro : MonoBehaviour
{
    [Header("対象")]
    public Transform player;


    [Header("挙動")]
    public float rotateSpeed = 8f;
    public bool lockYOnly = true;

    private Transform target;      // 今追いかける対象
    private bool isInside = false; // コライダー内にいるか

    void Update()
    {
        // コライダー内じゃないなら何もしない
        if (!isInside || target == null) return;

        Vector3 toTarget = target.position - transform.position;

        // 水平だけ向きたい場合（上下無視）
        if (lockYOnly) toTarget.y = 0f;

        // ほぼ同位置なら回転しない（ゼロ割対策）
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが入ったら追尾開始
        if (other.CompareTag("Player"))
        {
            target = other.transform;
            isInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーが出たら追尾停止
        if (other.CompareTag("Player"))
        {
            isInside = false;
            target = null;
        }
    }
}

