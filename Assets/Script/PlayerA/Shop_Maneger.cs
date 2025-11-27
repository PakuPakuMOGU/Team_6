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


    public LayerMask groundMask;      // Ground レイヤーのみ
    public float maxSlopeDeg = 45f;   // 設置許容斜面角


    public CenterRaycastSpaceApply cast;


    // レイ結果
    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

    // 直近の設置を戻す履歴
    private Stack<(Transform t, Vector3 pos, Quaternion rot)> placedHistory = new Stack<(Transform, Vector3, Quaternion)>();

    public GameObject Cancel_Button;


   /* private System.Collections.Generic.Stack<(Transform t, Vector3 pos, Quaternion rot)> placedHistory
           = new System.Collections.Generic.Stack<(Transform, Vector3, Quaternion)>();*/

    // クリック位置から地面を狙って Raycast（例）
    public bool TryGetGroundHit(out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitPoint = default;
        hitNormal = default;

        // 例：画面中央から前方へ。用途に応じてマウス座標や任意の原点に合わせてください。
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            // 法線が上向きか（斜度チェック）
            float cos = Vector3.Dot(hit.normal.normalized, Vector3.up);
            float slopeDeg = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            if (slopeDeg <= maxSlopeDeg)
            {
                hitPoint = hit.point;
                hitNormal = hit.normal.normalized;
                return true;
            }
        }
        return false;
    }



    void Start()
    {
        Cancel_Button.SetActive(false);

    }

    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        groundMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));



        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
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
        Cancel_Button.SetActive(true);
        cast.kono();

        while (targets != null && targetIndex < targets.Length && targets[targetIndex] == null)
            targetIndex++;
        if (targets == null || targetIndex >= targets.Length) return;

        Transform t = targets[targetIndex];
        if (t == null) return;

        // Undo用：設置前の状態を保存
        placedHistory.Push((t, t.position, t.rotation));

        // --- ここまではそのまま ---

        // ここから「savedPositionsの最後の位置」を使って設置
        if (cast != null && cast.savedPositions != null && cast.savedPositions.Count > 0)
        {
            Vector3 lastPos = cast.savedPositions[cast.savedPositions.Count - 1];

            // 直接ワールド座標に適用（center/normalの計算は使わず）
            t.position = lastPos;

            // 必要なら回転も保存してあるなら適用（なければコメントアウトのまま）
            // t.rotation = cast.savedRotations[cast.savedRotations.Count - 1];

            targetIndex++;
            return;
        }

        // フォールバック：savedPositionsが無い/空の場合は従来ロジックで設置（現在のコード）
        // ----- 以降はあなたの既存の half/lift 計算と delta適用 -----
        var box = t.GetComponentInChildren<BoxCollider>();
        float half = 0f;
        Vector3 currentCenterWorld = t.position;
        if (box != null)
        {
            currentCenterWorld = t.TransformPoint(box.center);
            Vector3 ext = Vector3.Scale(box.size * 0.5f, t.lossyScale);
            Vector3 n = HitNormal.normalized;
            half = Mathf.Abs(Vector3.Dot(n, t.right)) * ext.x +
                   Mathf.Abs(Vector3.Dot(n, t.up)) * ext.y +
                   Mathf.Abs(Vector3.Dot(n, t.forward)) * ext.z;
        }
        else
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                Vector3 c = b.center;
                Vector3 e = b.extents;
                currentCenterWorld = c;

                Vector3[] corners = new Vector3[]
                {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
                };
                float maxProj = 0f;
                Vector3 n = HitNormal.normalized;
                foreach (var wc in corners)
                {
                    float proj = Mathf.Abs(Vector3.Dot(wc - c, n));
                    maxProj = Mathf.Max(maxProj, proj);
                }
                half = maxProj;
            }
            else
            {
                half = 0.5f;
                currentCenterWorld = t.position;
            }
        }

        float lift = Mathf.Max(0f, extraLift);
        Vector3 desiredCenterWorld = HitPoint + HitNormal.normalized * (half + lift);
        Vector3 delta = desiredCenterWorld - currentCenterWorld;
        t.position += delta;

        targetIndex++;
    }






    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        last.t.position = last.pos;
        last.t.rotation = last.rot;

        targetIndex = Mathf.Max(targetIndex - 1, 0);

        Cancel_Button.SetActive(false);

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
