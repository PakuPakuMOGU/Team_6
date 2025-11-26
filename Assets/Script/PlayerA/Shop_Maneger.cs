
using System.Collections.Generic;
using UnityEngine;

public class Shop_Maneger : MonoBehaviour
{
    [Header("罠の在庫（1つずつ設置）")]
    [SerializeField] private Transform[] targets;
    private int targetIndex = 0; // 次に置くインデックス

    

    [Header("キャンセルボタン（任意）")]
    [SerializeField] private GameObject cancelButton;

    [Header("レイキャスト設定")]
    [SerializeField] private UnityEngine.Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float upOffset = 0.0f;     // 地面からの浮き上がり量（食い込み防止）
    [SerializeField] private bool alignToNormal = true; // 傾斜面に合わせる

    private LayerMask layerMask;

    // 中心レイの結果（毎フレーム更新）
    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

    // 直近の配置を元に戻すための履歴（最後に置いたものから戻す）
    private Stack<(Transform t, Vector3 pos, Quaternion rot)> placedHistory = new Stack<(Transform, Vector3, Quaternion)>();

    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        layerMask = LayerMask.GetMask("Ground"); // Groundレイヤーのみ
        if (cancelButton != null) cancelButton.SetActive(false);
    }

    void Update()
    {
        if (cam == null) return;

        // 画面中心からレイ生成
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Groundに対してレイキャスト
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            HitPoint = hit.point;     // 当たった座標
            HitNormal = hit.normal;   // 当たった面の法線
        }
        else
        {
            // ヒットなし → 中心方向のフォールバック（必要なら無効化ロジックに変更可）
            HitPoint = ray.GetPoint(maxDistance);
            HitNormal = -ray.direction;
        }
    }

    /// <summary>
    /// 購入・設置。次の targets[targetIndex] を中心レイの座標に置く。
    /// （UIボタンなどから呼び出し）
    /// </summary>
    public void BuyKagu()
    {
        // 置くものがない
        if (targets == null || targetIndex >= targets.Length) return;

        Transform t = targets[targetIndex];
        if (t == null)
        {
            // nullはスキップ
            targetIndex++;
            return;
        }

        // 置く前の位置・回転を履歴として保存（キャンセル用）
        placedHistory.Push((t, t.position, t.rotation));

        // 設置位置
        Vector3 p = HitPoint + HitNormal * upOffset;

        // 実際に設置
        t.position = p;

        if (alignToNormal)
        {
            // オブジェクトの上方向（t.up）を地面法線に合わせる
            t.rotation = Quaternion.FromToRotation(t.up, HitNormal) * t.rotation;

            // 前向き（forward）も制御したい場合は以下のようにLookRotationを使って調整できます：
            // Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, HitNormal).normalized;
            // if (forward.sqrMagnitude > 1e-6f) t.rotation = Quaternion.LookRotation(forward, HitNormal);
        }

        // 次のターゲットへ
        targetIndex++;

        // キャンセルボタンがあれば有効化
        if (cancelButton != null) cancelButton.SetActive(true);
    }

    /// <summary>
    /// 直前の設置をキャンセルして元に戻す。
    /// （UIボタンから呼び出し）
    /// </summary>
    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        last.t.position = last.pos;
        last.t.rotation = last.rot;

        // キャンセルした分、次に置くインデックスも戻す
        targetIndex = Mathf.Max(targetIndex - 1, 0);

        // 履歴が空になったらボタンを無効化
        if (placedHistory.Count == 0 && cancelButton != null)
            cancelButton.SetActive(false);
    }
}
