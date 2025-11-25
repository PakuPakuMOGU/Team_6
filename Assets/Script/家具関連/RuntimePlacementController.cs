
using UnityEngine;
using UnityEngine.EventSystems;
using UCamera = UnityEngine.Camera;

public class RuntimePlacementController : MonoBehaviour
{
    public enum SurfaceMode { Upright, AlignToSurface }

    [Header("設置する家具Prefab（FurnitureSelectorByTag から切り替え）")]
    public GameObject furniturePrefab;

    [Header("プレビュー材質（半透明など任意）")]
    public Material ghostMaterial;

    [Header("地面レイヤー")]
    public LayerMask groundLayer;

    [Header("グリッド・角度スナップ")]
    public float gridSize = 1.0f;
    public float angleSnap = 15f;

    [Header("傾き上限（Align適用時）")]
    public float maxTiltDegrees = 10f;

    [Header("高さ補正（Pivotが底面でない場合）")]
    public float heightOffset = 0f;

    [Header("重なり防止（OverlapBox用の余白）")]
    public Vector3 overlapMargin = new Vector3(0.01f, 0.01f, 0.01f);

    private bool placeMode = false;
    private GameObject ghostInstance;
    private float currentYRotation = 0f;
    private bool snapGridOn = true;
    private bool snapAngleOn = true;
    private SurfaceMode surfaceMode = SurfaceMode.Upright;

    // 公開プロパティ（必要ならUIから参照）
    public bool PlaceModeActive => placeMode;

    // 明示的にON/OFF
    public void SetPlaceMode(bool on)
    {
        if (on == placeMode)
        {
            if (placeMode) RefreshGhostWithCurrentPrefab();
            return;
        }

        placeMode = on;
        if (placeMode) CreateGhost();
        else DestroyGhost();

        Debug.Log($"PlaceMode: {placeMode}");
    }

    public void TogglePlaceMode() => SetPlaceMode(!placeMode);

    // 表面モード（UIボタン用）
    public void SetSurfaceMode_Upright() => surfaceMode = SurfaceMode.Upright;
    public void SetSurfaceMode_AlignSurface() => surfaceMode = SurfaceMode.AlignToSurface;

    // タグ選択時のゴースト差し替え用
    public void RefreshGhostWithCurrentPrefab()
    {
        if (ghostInstance == null) return;
        DestroyGhost();
        CreateGhost();
    }

    private void Update()
    {
        if (!placeMode) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        UpdateGhostFollowMouse();

        // マウスホイールでY回転
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            currentYRotation += scroll * 10f; // 1目盛り=10度
            if (snapAngleOn && angleSnap > 0f)
                currentYRotation = Mathf.Round(currentYRotation / angleSnap) * angleSnap;
            ApplyGhostRotationYaw();
        }

        // スナップ切替
        if (Input.GetKeyDown(KeyCode.T)) snapGridOn = !snapGridOn;
        if (Input.GetKeyDown(KeyCode.Y)) snapAngleOn = !snapAngleOn;

        // 設置確定
        if (Input.GetMouseButtonDown(0))
            TryPlaceFromGhost();

        // キャンセル（右クリック）
        if (Input.GetMouseButtonDown(1))
            SetPlaceMode(false);

        // ショートカットでモード切替
        if (Input.GetKeyDown(KeyCode.U)) surfaceMode = SurfaceMode.Upright;
        if (Input.GetKeyDown(KeyCode.A)) surfaceMode = SurfaceMode.AlignToSurface;
    }

    private void CreateGhost()
    {
        if (furniturePrefab == null)
        {
            Debug.LogWarning("furniturePrefab が未設定です。");
            return;
        }
        ghostInstance = Instantiate(furniturePrefab);
        SetGhostAppearance();
        currentYRotation = 0f;
    }

    private void DestroyGhost()
    {
        if (ghostInstance) Destroy(ghostInstance);
        ghostInstance = null;
    }

    private void SetGhostAppearance()
    {
        if (!ghostInstance) return;

        // 物理干渉しないよう Collider はオフ
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // 半透明材質
        if (ghostMaterial)
        {
            foreach (var r in ghostInstance.GetComponentsInChildren<Renderer>())
                r.material = ghostMaterial;
        }
        else
        {
            foreach (var r in ghostInstance.GetComponentsInChildren<Renderer>())
            {
                if (r.material.HasProperty("_Color"))
                {
                    var c = r.material.color;
                    c.a = 0.6f;
                    r.material.color = c;
                }
            }
        }
    }

    private void UpdateGhostFollowMouse()
    {
        if (!ghostInstance) return;

        var cam = UCamera.main;
        if (!cam)
        {
            cam = Object.FindObjectOfType<UCamera>();
            if (!cam) return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 pos = hit.point;

            if (snapGridOn && gridSize > 0f)
            {
                pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
            }
            pos.y += heightOffset;

            Quaternion rot = ComputeRotation(hit);

            ghostInstance.transform.SetPositionAndRotation(pos, rot);
            ApplyGhostRotationYaw(); // 最後にY回転を適用
        }
    }

    private Quaternion ComputeRotation(RaycastHit hit)
    {
        if (surfaceMode == SurfaceMode.Upright)
        {
            return Quaternion.identity;
        }
        else // AlignToSurface
        {
            Quaternion desired = Quaternion.FromToRotation(Vector3.up, hit.normal);
            return LimitTilt(desired, maxTiltDegrees);
        }
    }

    private Quaternion LimitTilt(Quaternion desiredTilt, float maxDegrees)
    {
        Vector3 upAfter = desiredTilt * Vector3.up;
        float angle = Vector3.Angle(Vector3.up, upAfter);
        if (angle <= maxDegrees || maxDegrees <= 0f) return desiredTilt;
        float t = maxDegrees / angle;
        return Quaternion.Slerp(Quaternion.identity, desiredTilt, t);
    }

    private void ApplyGhostRotationYaw()
    {
        if (!ghostInstance) return;

        Vector3 e = ghostInstance.transform.eulerAngles;
        e.y = currentYRotation;
        if (snapAngleOn && angleSnap > 0f)
            e.y = Mathf.Round(e.y / angleSnap) * angleSnap;
        ghostInstance.transform.rotation = Quaternion.Euler(e);
    }

    private void TryPlaceFromGhost()
    {
        if (!ghostInstance || !furniturePrefab) return;

        Vector3 pos = ghostInstance.transform.position;
        Quaternion rot = ghostInstance.transform.rotation;

        // OverlapBoxで重なりチェック（レンダラの合成Bounds）
        var (center, halfExtents) = GetCompositeBoundsWorld(ghostInstance);
        center = pos + (center - ghostInstance.transform.position);
        Vector3 he = halfExtents + overlapMargin;

        Collider[] hits = Physics.OverlapBox(center, he, rot);
        if (hits.Length > 0)
        {
            Debug.Log("重なり検出のため設置不可");
            return;
        }

        GameObject placed = Instantiate(furniturePrefab, pos, rot);

        // 編集用コンポーネント付与
        var editable = placed.AddComponent<RuntimeEditableObject>();
        editable.groundLayer = groundLayer;
        editable.gridSize = gridSize;
        editable.angleSnap = angleSnap;
        editable.maxTiltDegrees = maxTiltDegrees;

        Debug.Log("設置完了");
        // 単発で終了したい場合は以下をON
        // SetPlaceMode(false);
    }

    private (Vector3 center, Vector3 halfExtents) GetCompositeBoundsWorld(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return (go.transform.position, Vector3.one * 0.5f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return (b.center, b.extents);
    }
}
