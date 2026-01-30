using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// 設置時は「回転を絶対に変えず」、位置のみ
/// ・まずは savedPositions があるならそこに復元（回転維持）
/// ・なければ cameraAnchor（カメラに付随する空オブジェクト）の位置へ 1 回スナップ
/// フォールバックとして HitPoint/HitNormal による床合わせも可能
/// </summary>
public class Shop_Maneger : MonoBehaviour, INetworkRunnerCallbacks
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

    [Header("在庫でかタレット")]
    [SerializeField] public Transform[] targets6;
    public int targetIndex6 = 0;

    [Header("在庫チビタレット")]
    [SerializeField] public Transform[] targets7;
    public int targetIndex7 = 0;

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

    // ネットワーク.
    private NetworkRunner _runner;
    [SerializeField] private NetworkPrefabRef fence1Prefab;
    [SerializeField] private NetworkPrefabRef fence2Prefab;
    [SerializeField] private NetworkPrefabRef landMinePrefab;
    [SerializeField] private NetworkPrefabRef roboHandPrefab;
    [SerializeField] private NetworkPrefabRef roboGunPrefab;
    [SerializeField] private NetworkPrefabRef roboSpecialPrefab;
    [SerializeField] private NetworkPrefabRef turretBigPrefab;
    [SerializeField] private NetworkPrefabRef turretSmallPrefab;

    private NetworkPrefabRef GetPrefabByTag(string tag)
    {
        switch (tag)
        {
            case "Fence1": return fence1Prefab;
            case "Fence2": return fence2Prefab;
            case "Land": return landMinePrefab;
            case "S_Robo": return roboHandPrefab;
            case "G_Robo": return roboGunPrefab;
            case "T_Robo": return roboSpecialPrefab;
            case "B_Tare": return turretBigPrefab;
            case "C_Tare": return turretSmallPrefab;
        }

        Debug.LogError($"[Shop_Manager] 未対応のタグ: {tag}");
        return default;
    }

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
        // ネットワーク、Runnerが見つかるまで待機.
        StartCoroutine(WaitForRunner());

        if (Button_Canbus != null) Button_Canbus.SetActive(false);
        CurrentTarget = null;
    }
    private System.Collections.IEnumerator WaitForRunner()
    {
        // Runnerが見つかるまで動作.
        while (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
            yield return null;
        }

        Debug.Log("[Shop] Runner 発見！Callbacks 登録！");
        _runner.AddCallbacks(this);
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

            case "B_Tare":
                placed = TryPlaceFromArray(targets6, ref targetIndex6, tagName);
                break;

            case "C_Tare":
                placed = TryPlaceFromArray(targets7, ref targetIndex7, tagName);
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
        var t = CurrentTarget;

        if (t != null)
        {
            // タグ名取得（配列探索が不要なら、Transform.tagで足ります）
            string tagName;
            int zeroBasedIndex;

            if (!TryFindArrayAndIndexOf(t, out tagName, out zeroBasedIndex))
            {
                tagName = t.tag;
            }

            // ★ タグ別の重力適用ルール
            switch (tagName)
            {
                case "S_Robo":
                case "G_Robo":
                case "T_Robo":
                case "Land":
                case "B_Tare":
                case "C_Tare":
                    // ロボ系は重力ON
                    EnableGravity(t, includeChildren: true, alsoMakeDynamic: true);
                    Debug.Log($"[Shop_Maneger] Hensyu: tag={tagName} → useGravity ON");
                    break;

                case "Fence1":
                case "Fence2":
                    // 柵は重力OFFにするなど（必要なら）
                    // DisableGravity(t, includeChildren: true, makeKinematic: true);
                    break;

                default:
                    break;
            }
        }

        // ← 確定処理のあとに UI を閉じる
        CurrentTarget = null;
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    /// <summary>
    /// 現在の在庫配列群（targets～targets7）から、指定Transformの所属配列タグと添字（0ベース）を探索。
    /// 見つかれば true と tagName/zeroBasedIndex を返す。
    /// </summary>
    private bool TryFindArrayAndIndexOf(Transform target, out string tagName, out int zeroBasedIndex)
    {
        (Transform[] arr, string tag)[] groups = new (Transform[], string)[]
        {
            (targets,  "Fence2"),
            (targets1, "Fence1"),
            (targets2, "Land"),
            (targets3, "S_Robo"),
            (targets4, "G_Robo"),
            (targets5, "T_Robo"),
            (targets6, "B_Tare"),
            (targets7, "C_Tare"),
        };

        for (int g = 0; g < groups.Length; g++)
        {
            var (arr, tag) = groups[g];
            if (arr == null) continue;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == target)
                {
                    tagName = tag;
                    zeroBasedIndex = i;
                    return true;
                }
            }
        }

        tagName = null;
        zeroBasedIndex = -1;
        return false;
    }

    /// <summary>
    /// 対象Transform（と任意で子階層）のRigidbodyに対して useGravity を true にし、
    /// 必要なら isKinematic を false に戻す。戻り値は変更を適用した Rigidbody の件数。
    /// </summary>
    private int EnableGravity(Transform root, bool includeChildren, bool alsoMakeDynamic)
    {
        int count = 0;

        if (root == null) return 0;

        if (includeChildren)
        {
            var rbs = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++)
            {
                var rb = rbs[i];
                rb.useGravity = true;
                if (alsoMakeDynamic) rb.isKinematic = false;
                count++;
            }
        }
        else
        {
            var rb = root.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                if (alsoMakeDynamic) rb.isKinematic = false;
                count = 1;
            }
        }
        return count;
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

            case "B_Tare":
                targetIndex6 = Mathf.Max(targetIndex6 - 1, 0);
                break;

            case "C_Tare":
                targetIndex7 = Mathf.Max(targetIndex7 - 1, 0);
                break;
        }

        CurrentTarget = null;
        if (Button_Canbus != null) Button_Canbus.SetActive(false);
    }

    // ------------------ 共通：在庫配列から設置（A: アンカーへ1回スナップ） ------------------

    private bool TryPlaceFromArray(Transform[] arr, ref int index, string tagName)
    {
        if (arr == null || arr.Length == 0)
            return false;

        while (index < arr.Length && arr[index] == null) index++;
        if (index >= arr.Length)
            return false;

        Transform t = arr[index];
        if (t == null) return false;

        Vector3 pos = (cameraAnchor != null) ? cameraAnchor.position : HitPoint;
        Quaternion rot = t.rotation;

        // ★ Host に配置を依頼
        RPC_RequestPlace(tagName, pos, rot);

        index++;
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestPlace(string tag, Vector3 pos, Quaternion rot)
    {
        var prefab = GetPrefabByTag(tag);
        _runner.Spawn(prefab, pos, rot);
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
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
}
