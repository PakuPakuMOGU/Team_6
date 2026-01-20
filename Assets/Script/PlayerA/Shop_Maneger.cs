
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 設置時は「回転を絶対に変えず」、位置のみ
/// ・まずは savedPositions があるならそこに復元（回転維持）
/// ・なければ cameraAnchor（カメラに付随する空オブジェクト）の位置へ 1 回スナップ
/// フォールバックとして HitPoint/HitNormal による床合わせも可能
/// </summary>
public class Shop_Maneger : MonoBehaviour
{
    [Header("在庫柵1")]
    [SerializeField] public Transform[] targets;
    public int targetIndex = 0;

    [Header("在庫柵2")]
    [SerializeField] public Transform[] targets1;
    public int targetIndex1 = 0;

    [Header("在庫地雷")]
    [SerializeField] public Transform[] targets2;
    public int targetIndex2 = 0;

    [Header("在庫素手ロボット")]
    [SerializeField] public Transform[] targets3;
    public int targetIndex3 = 0;

    [Header("在庫銃ロボット")]
    [SerializeField] public Transform[] targets4;
    public int targetIndex4 = 0;

    [Header("在庫特殊ロボット")]
    [SerializeField] public Transform[] targets5;
    public int targetIndex5 = 0;

    [Header("レイキャスト設定")]
    [SerializeField] private UnityEngine.Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool alignToNormal = false; // ※方針的に使わない（常に回転固定）
    [Tooltip("Pivotが底面でない場合の持ち上げ補正（めり込み防止）")]
    [SerializeField] private float extraLift = 0.01f;
    public LayerMask groundMask;
    public float maxSlopeDeg = 45f;

    [Header("配置アンカー（カメラの子にした空オブジェクト）")]
    [SerializeField] private Transform cameraAnchor;

    public CenterRaycastSpaceApply cast;

    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

    private struct PlacedRecord
    {
        public Transform t;
        public Vector3 pos;      // Cancelで戻すための「設置前の位置」
        public Quaternion rot;   // Cancelで戻すための「設置前の回転」
        public string tagName;
    }
    private readonly Stack<PlacedRecord> placedHistory = new();

    public GameObject Button_Canbus;

    // 直近に設置・編集中の対象（UIButtonNudgeなどから参照）
    public Transform CurrentTarget { get; private set; }

    // ------------------ ライフサイクル ------------------
    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        // Ground レイヤーが未指定ならデフォルトで "Ground"
        if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
    }

    void Start()
    {
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
        CurrentTarget = null;
    }

    void Update()
    {
        if (cam == null) return;

        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            float cos = Vector3.Dot(hit.normal.normalized, Vector3.up);
            float slopeDeg = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            if (slopeDeg <= maxSlopeDeg)
            {
                HitPoint = hit.point;
                HitNormal = hit.normal.normalized;
            }
            else
            {
                // 急斜面はノーマルを上向き扱い
                HitPoint = hit.point;
                HitNormal = Vector3.up;
            }
        }
        else
        {
            // 非ヒット時は見通し線の先端位置＋ノーマルは上向き
            HitPoint = ray.GetPoint(maxDistance);
            HitNormal = Vector3.up;
        }
    }

    // ------------------ ボタン（タグ）から呼ぶ入口 ------------------
    public void BuyByTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            Debug.LogWarning("[Shop_Maneger] BuyByTag: tagName が空です");
            return;
        }

        if (cast != null) cast.kono(); // 既存の前処理があるなら実行

        bool placed = false;

        // タグ名で在庫配列を選択
        switch (tagName)
        {
            case "Fence2":
                placed = TryPlaceFromArray(targets, ref targetIndex, tagName);
                break;

            case "Fence1":
                placed = TryPlaceFromArray(targets1, ref targetIndex1, tagName);
                break;

            case "Land":
                placed = TryPlaceFromArray(targets2, ref targetIndex2, tagName);
                break;

            case "S_Robo":
                placed = TryPlaceFromArray(targets3, ref targetIndex3, tagName);
                break;

            case "G_Robo":
                placed = TryPlaceFromArray(targets4, ref targetIndex4, tagName);
                break;

            case "T_Robo":
                placed = TryPlaceFromArray(targets5, ref targetIndex5, tagName);
                break;

            default:
                Debug.LogWarning($"[Shop_Maneger] 未対応のタグ『{tagName}』です。switch に追加してください。");
                break;
        }

        if (!placed && Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    // 編集終了（UIを閉じる）
    public void Hensyu()
    {
        CurrentTarget = null;
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    // 直前の設置を取り消す（Cancel）
    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        if (last.t != null)
        {
            last.t.SetPositionAndRotation(last.pos, last.rot);
        }

        // 種類ごとに在庫インデックスを戻す（下限保護）
        switch (last.tagName)
        {
            case "Fence2":
                targetIndex = Mathf.Max(targetIndex - 1, 0);
                break;
            case "Fence1":
                targetIndex1 = Mathf.Max(targetIndex1 - 1, 0);
                break;
            case "Land":
                targetIndex2 = Mathf.Max(targetIndex2 - 1, 0);
                break;

            case "S_Robo":
                targetIndex3 = Mathf.Max(targetIndex3 - 1, 0);
                break;

            case "G_Robo":
                targetIndex4 = Mathf.Max(targetIndex4 - 1, 0);
                break;

            case "T_Robo":
                targetIndex5 = Mathf.Max(targetIndex5 - 1, 0);
                break;
        }

        CurrentTarget = null;
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    // ------------------ 共通：在庫配列から設置（A: アンカーへ1回スナップ） ------------------
    private bool TryPlaceFromArray(Transform[] arr, ref int index, string tagNameForHistory)
    {
        // 在庫が空／存在しない
        if (arr == null || arr.Length == 0)
        {
            Debug.LogWarning($"[Shop_Maneger] 在庫がありません（{tagNameForHistory}）");
            return false;
        }

        // null在庫スキップ（連続null対応）
        while (index < arr.Length && arr[index] == null) index++;

        // インデックス境界チェック
        if (index >= arr.Length)
        {
            Debug.Log($"[Shop_Maneger] {tagNameForHistory} は在庫切れです");
            return false;
        }

        Transform t = arr[index];
        if (t == null) return false;

        // ★ 設置前の状態を保持（Cancel用）
        Vector3 prePos = t.position;
        Quaternion preRot = t.rotation;

        // ★ 回転は固定（設置後も preRot のまま）
        Quaternion originalRotation = preRot;

        // --- savedPositions がある場合は位置のみ復元（回転は維持） ---
        if (cast != null && cast.savedPositions != null && cast.savedPositions.Count > 0)
        {
            Vector3 lastPos = cast.savedPositions[^1];
            t.SetPositionAndRotation(lastPos, originalRotation);

            // 成功として履歴に積む（設置前の位置・回転）
            placedHistory.Push(new PlacedRecord
            {
                t = t,
                pos = prePos,
                rot = preRot,
                tagName = tagNameForHistory
            });

            CurrentTarget = t;
            index++;
            Button_Canbus?.SetActive(true);
            return true;
        }

        // --- 合成Boundsで中心・高さ半分を取得（底面めり込み対策に利用） ---
        Bounds worldBounds;
        if (!TryGetWorldBounds(t, out worldBounds))
        {
            // 取得できない場合はPivotベース
            worldBounds = new Bounds(t.position, Vector3.zero);
        }

        float halfHeight = Mathf.Max(worldBounds.extents.y, 0f);
        float lift = Mathf.Max(0f, extraLift);

        // ★ A: アンカーへ 1 回スナップ（アンカー未設定なら HitPoint へフォールバック）
        Vector3 basePoint = (cameraAnchor != null) ? cameraAnchor.position : HitPoint;
        Vector3 upNormal = Vector3.up; // 回転固定方針なのでノーマルは上向きで扱う

        Vector3 desiredCenterWorld = basePoint + upNormal * (halfHeight + lift);
        Vector3 currentCenterWorld = worldBounds.center;
        Vector3 delta = desiredCenterWorld - currentCenterWorld;

        // ★ 位置のみ更新、回転は originalRotation に固定
        t.SetPositionAndRotation(t.position + delta, originalRotation);

        // ※ alignToNormal は使わない（回転固定方針）
        // if (alignToNormal) { ... } // 完全無効

        // 成功として履歴に積む（設置前の位置・回転）
        placedHistory.Push(new PlacedRecord
        {
            t = t,
            pos = prePos,    // Cancelで元位置に戻せる
            rot = preRot,
            tagName = tagNameForHistory
        });

        CurrentTarget = t;
        index++;
        Button_Canbus?.SetActive(true);
        return true;
    }

    /// <summary>
    /// 対象Transform配下の Renderer / Collider から合成Boundsを取得
    /// ワールドAABBとして Encapsulate していきます
    /// </summary>
    private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool initialized = false;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!initialized)
            {
                bounds = r.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            var c = colliders[i];
            if (!initialized)
            {
                bounds = c.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return initialized;
    }
}