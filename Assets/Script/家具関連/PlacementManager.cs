
using System.Collections.Generic;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Placement Settings")]
    public Material ghostMaterial;       // 設置中の見た目にするマテリアル
    public float moveSpeed = 3f;         // WASD移動速度
    public float rotationSpeed = 120f;   // F/G回転速度（deg/sec）
    public float height = 0f;            // 設置するY高さ（地面がy=0なら0）
    public bool useGridSnap = false;     // グリッドスナップ有効/無効
    public float gridSize = 1f;          // スナップ間隔

    private GameObject currentObj;                       // 今設置中のオブジェクト
    private readonly List<Renderer> renderers = new();   // 子を含むレンダラ
    private readonly Dictionary<Renderer, Material[]> originalMats = new(); // 元マテリアル
    private bool isPlacing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>購入ボタンから呼び出して設置モード開始</summary>
    public void StartPlacement(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab が指定されていません。");
            return;
        }
        if (ghostMaterial == null)
        {
            Debug.LogWarning("ghostMaterial が指定されていません。");
            return;
        }

        // 生成＆初期位置
        currentObj = Instantiate(prefab);
        currentObj.transform.position = new Vector3(0f, height, 0f);

        // 設置中は全Rendererをゴースト化（元マテリアルを保持）
        renderers.Clear();
        originalMats.Clear();
        foreach (var r in currentObj.GetComponentsInChildren<Renderer>())
        {
            renderers.Add(r);
            originalMats[r] = r.sharedMaterials;

            var ghostArray = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < ghostArray.Length; i++) ghostArray[i] = ghostMaterial;
            r.sharedMaterials = ghostArray; // ゴースト差し替え
        }

        // 設置中は衝突しないようColliderを無効化
        foreach (var col in currentObj.GetComponentsInChildren<Collider>()) col.enabled = false;

        isPlacing = true;
    }

    private void Update()
    {
        if (!isPlacing || currentObj == null) return;

        HandleMove();
        HandleRotate();

        // 確定：左クリック or Space
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmPlacement();
        }
        // キャンセル：右クリック or Esc
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    private void HandleMove()
    {
        Vector3 delta = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) delta += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) delta += Vector3.back;
        if (Input.GetKey(KeyCode.A)) delta += Vector3.left;
        if (Input.GetKey(KeyCode.D)) delta += Vector3.right;

        if (delta.sqrMagnitude > 0f)
        {
            delta = delta.normalized * moveSpeed * Time.deltaTime;
            Vector3 p = currentObj.transform.position + new Vector3(delta.x, 0f, delta.z);
            p.y = height;

            if (useGridSnap)
            {
                p.x = Mathf.Round(p.x / gridSize) * gridSize;
                p.z = Mathf.Round(p.z / gridSize) * gridSize;
            }
            currentObj.transform.position = p;
        }
    }

    private void HandleRotate()
    {
        float rot = 0f;
        if (Input.GetKey(KeyCode.F)) rot -= rotationSpeed * Time.deltaTime; // 左回転
        if (Input.GetKey(KeyCode.G)) rot += rotationSpeed * Time.deltaTime; // 右回転

        if (Mathf.Abs(rot) > 0f)
        {
            currentObj.transform.Rotate(0f, rot, 0f, Space.World);
        }
    }

    /// <summary>設置確定：元マテリアルに戻し、Collider有効化、編集不可化</summary>
    private void ConfirmPlacement()
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (originalMats.TryGetValue(r, out var mats))
            {
                r.sharedMaterials = mats; // 元に戻す
            }
        }

        foreach (var col in currentObj.GetComponentsInChildren<Collider>()) col.enabled = true;

        // 編集不可の印（必要なら他スクリプトでこの印をチェック）
        currentObj.AddComponent<PlacedLock>();

        // クリア（次の購入でまた置ける）
        currentObj = null;
        renderers.Clear();
        originalMats.Clear();
        isPlacing = false;
    }

    /// <summary>設置キャンセル：生成したものを破棄</summary>
    private void CancelPlacement()
    {
        if (currentObj != null) Destroy(currentObj);
        currentObj = null;
        renderers.Clear();
        originalMats.Clear();
        isPlacing = false;
    }
}
