
using System.Collections.Generic;
using UnityEngine;

public class Shop_Maneger : MonoBehaviour
{
    [Header("罠の在庫（1つずつ設置）")]
    [SerializeField] private Transform[] targets;
    private int targetIndex = 0;

    [Header("レイキャスト設定")]
    [SerializeField] private UnityEngine.Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool alignToNormal = true; // 傾斜面に合わせる

    [Tooltip("Pivotが底面なら0のままでOK。Pivotが中心の場合、モデルの半径を自動計算して補正します。")]
    [SerializeField] private float extraLift = 0.01f; // わずかな浮かせ（食い込み防止）

    private LayerMask layerMask;

    // レイ結果
    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

    // 直近の設置を戻す履歴
    private Stack<(Transform t, Vector3 pos, Quaternion rot)> placedHistory = new Stack<(Transform, Vector3, Quaternion)>();

    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        layerMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            HitPoint = hit.point;
            HitNormal = hit.normal;
        }
        else
        {
            HitPoint = ray.GetPoint(maxDistance);
            HitNormal = -ray.direction;
        }
    }

    /// <summary>
    /// 1つ設置（UIボタンやキーから呼び出し）
    /// </summary>
    public void BuyKagu()
    {
        if (targets == null || targetIndex >= targets.Length) return;

        Transform t = targets[targetIndex];
        if (t == null) { targetIndex++; return; }

        // 設置前の状態を保存（Cancel用）
        placedHistory.Push((t, t.position, t.rotation));

        // モデルの「半径（半サイズ）」を法線方向に投影した長さを計算
        float halfSizeAlongNormal = ComputeHalfExtentAlongNormal(t, HitNormal);

        // 設置位置：地面ヒット点 + 法線方向へ半サイズ + 微小リフト
        Vector3 placePos = HitPoint + HitNormal * (halfSizeAlongNormal + extraLift);

        // 位置
        t.position = placePos;

        // 回転（傾斜に合わせる）
        if (alignToNormal)
        {
            // 既存のupを地面法線へ合わせる
            t.rotation = Quaternion.FromToRotation(t.up, HitNormal) * t.rotation;

            // 「前向き」をカメラの水平前方に揃えたい場合は以下を追加（必要なら）
            // Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, HitNormal).normalized;
            // if (forward.sqrMagnitude > 1e-6f)
            //     t.rotation = Quaternion.LookRotation(forward, HitNormal);
        }

        targetIndex++;
    }

    /// <summary>
    /// 直前の設置を取り消して元の位置へ戻す
    /// </summary>
    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        last.t.position = last.pos;
        last.t.rotation = last.rot;

        targetIndex = Mathf.Max(targetIndex - 1, 0);
    }

    /// <summary>
    /// モデルの半サイズ（bounds.extents）を法線方向に投影した長さを返す。
    /// Pivotが中心でも底面でも、だいたい正しい接地オフセットが得られる。
    /// </summary>
    private float ComputeHalfExtentAlongNormal(Transform t, Vector3 normal)
    {
        // 優先：Collider → 次点：Renderer
        Collider col = t.GetComponentInChildren<Collider>();
        if (col != null)
        {
            var e = col.bounds.extents;
            return Mathf.Abs(normal.x) * e.x + Mathf.Abs(normal.y) * e.y + Mathf.Abs(normal.z) * e.z;
        }

        Renderer rend = t.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var e = rend.bounds.extents;
            return Mathf.Abs(normal.x) * e.x + Mathf.Abs(normal.y) * e.y + Mathf.Abs(normal.z) * e.z;
        }

        // 見つからない場合は0（Pivotが底面だと仮定）
        return 0f;
    }
}
