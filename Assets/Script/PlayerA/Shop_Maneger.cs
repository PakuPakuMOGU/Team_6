
using System.Collections.Generic;
using UnityEngine;

public class Shop_Maneger : MonoBehaviour
{
    [Header("在庫（罠）")]
    [SerializeField] public Transform[] targets;
    public int targetIndex = 0;

    [Header("在庫（家具A：椅子など）")]
    [SerializeField] public Transform[] targets1;
    public int targetIndex1 = 0;

    [Header("在庫（家具B：テーブルなど）")]
    [SerializeField] public Transform[] targets2;
    public int targetIndex2 = 0;

    [Header("レイキャスト設定")]
    [SerializeField] private UnityEngine.Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool alignToNormal = false;
    [Tooltip("Pivotが底面でない場合の持ち上げ補正")]
    [SerializeField] private float extraLift = 0.01f;
    public LayerMask groundMask;
    public float maxSlopeDeg = 45f;

    public CenterRaycastSpaceApply cast;

    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

    private struct PlacedRecord
    {
        public Transform t;
        public Vector3 pos;
        public Quaternion rot;
        public string tagName;
    }
    private readonly Stack<PlacedRecord> placedHistory = new();

    public GameObject Button_Canbus;

    // ★ 直近に設置・編集中の対象（UIButtonNudgeから参照）
    public Transform CurrentTarget { get; private set; }

    // ------------------ 基本ライフサイクル ------------------
    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        // Ground レイヤー運用なら明示
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
                // 急斜面はとりあえず位置だけ更新
                HitPoint = hit.point;
                HitNormal = Vector3.up;
            }
        }
        else
        {
            HitPoint = ray.GetPoint(maxDistance);
            HitNormal = -ray.direction;
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

        // ★ タグ名で配列を選ぶ（タグ名はボタン側と一致させてください）
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

            default:
                Debug.LogWarning($"[Shop_Maneger] 未対応のタグ『{tagName}』です。switch に追加してください。");
                break;
        }

        if (!placed && Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    public void Hensyu()
    {
        // 編集UIを閉じる際は対象をクリア（誤操作防止）
        CurrentTarget = null;
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        if (last.t != null)
        {
            last.t.position = last.pos;
            last.t.rotation = last.rot;
        }

        // 種類ごとにインデックスを戻す
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
        }

        // 取り消し後は編集中対象をクリア
        CurrentTarget = null;

        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    // ------------------ 共通：在庫配列から設置 ------------------

    private bool TryPlaceFromArray(Transform[] arr, ref int index, string tagNameForHistory)
    {
        // ...（nullスキップなど既存処理）

        Transform t = arr[index];
        if (t == null) return false;

        // ★ 1) 現在のワールド回転を保存
        Quaternion originalRotation = t.rotation;

        // 履歴（Cancel用）にも元回転を保存
        placedHistory.Push(new PlacedRecord
        {
            t = t,
            pos = t.position,
            rot = originalRotation,
            tagName = tagNameForHistory
        });

        // savedPositions がある場合は位置のみ復元（回転は維持）
        if (cast != null && cast.savedPositions != null && cast.savedPositions.Count > 0)
        {
            Vector3 lastPos = cast.savedPositions[^1];

            // ★ 2) 回転は originalRotation のまま、位置だけ変更
            t.SetPositionAndRotation(lastPos, originalRotation);

            CurrentTarget = t;
            index++;
            Button_Canbus?.SetActive(true);
            return true;
        }

        // --- 半径・中心の算出（あなたの既存コードのままでOK） ---
        float half = 0f;
        Vector3 currentCenterWorld = t.position;
        // （BoxCollider / Renderer から center や extents を計算する既存部分をそのまま使ってください）

        float lift = Mathf.Max(0f, extraLift);
        Vector3 desiredCenterWorld = HitPoint + HitNormal.normalized * (half + lift);
        Vector3 delta = desiredCenterWorld - currentCenterWorld;

        // ★ 3) 位置のみ更新、回転は originalRotation に固定
        t.SetPositionAndRotation(t.position + delta, originalRotation);

        // ★ 4) ここで自動回転は絶対にしない（ブロックを完全コメントアウト）
        // if (alignToNormal)
        // {
        //     Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, HitNormal).normalized;
        //     if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        //     t.rotation = Quaternion.LookRotation(forward, HitNormal);
        // }

        CurrentTarget = t;
        index++;
        Button_Canbus?.SetActive(true);
        return true;
    }

}
