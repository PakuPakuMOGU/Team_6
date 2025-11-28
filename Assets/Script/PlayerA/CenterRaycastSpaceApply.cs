
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// スペースキーで：
/// 1) カメラ中心からレイキャスト
/// 2) ヒットした座標を保存（原点 or 衝突点）
/// 3) （任意）applyTarget にその座標を適用（ワールド or ローカル、即時 or スムーズ）
/// 4) PlayerPrefs にも保存（JSON）
/// </summary>
public class CenterRaycastSpaceApply : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("レイを飛ばすカメラ。空なら Camera.main を使用")]
    public UnityEngine.Camera targetCamera;

    [Tooltip("最大距離")]
    public float maxDistance = 100f;

    [Tooltip("対象レイヤー")]
    public LayerMask layerMask = ~0; // 既定は全レイヤー

    [Header("Save Settings")]
    [Tooltip("true: オブジェクト原点(transform.position)を保存 / false: 衝突点(hit.point)を保存")]
    public bool saveObjectOrigin = true;

    [Header("Apply Settings")]
    [Tooltip("適用対象（ここに設定した Transform に座標を適用）。未設定なら保存のみ")]
    public Transform applyTarget;

    [Tooltip("適用時の座標系: true=ワールド座標 / false=ローカル座標（親基準）")]
    public bool applyAsWorldPosition = true;

    [Tooltip("スムーズに移動（Lerp）")]
    public bool smoothApply = false;

    [Tooltip("スムーズ移動の速度係数（大きいほど速い）")]
    public float smoothSpeed = 8f;

    [Header("Debug")]
    public bool debugLog = true;

    // 保存された座標リスト（実行中）
    public List<Vector3> savedPositions = new List<Vector3>();

    // スムーズ移動用
    private bool isMoving = false;
    private Vector3 moveTargetPos;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("[CenterRaycastSpaceApply] Camera が見つかりません。targetCamera を設定してください。");
        }
    }

    public void kono()
    {
        // --- スペースキーで一括処理 ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DoRaycastSaveAndApply();
        }

        // --- スムーズ移動処理 ---
        if (smoothApply && isMoving && applyTarget != null)
        {
            Vector3 current = applyAsWorldPosition ? applyTarget.position : applyTarget.localPosition;
            Vector3 next = Vector3.Lerp(current, moveTargetPos, Time.deltaTime * smoothSpeed);

            if (applyAsWorldPosition) applyTarget.position = next;
            else applyTarget.localPosition = next;

            if (Vector3.Distance(current, moveTargetPos) < 0.01f)
            {
                isMoving = false;
                if (debugLog) Debug.Log("[CenterRaycastSpaceApply] スムーズ適用完了");
            }
        }
    }

    /// <summary>
    /// 中心レイキャスト → 座標保存 →（任意）適用 → PlayerPrefs保存
    /// </summary>
    private void DoRaycastSaveAndApply()
    {
        if (targetCamera == null) return;

        // 画面中心からレイ
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = targetCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.transform;

            // 保存する座標（原点 or 衝突点）
            Vector3 positionToSave = saveObjectOrigin ? hitTransform.position : hit.point;

            savedPositions.Add(positionToSave);
            if (debugLog) Debug.Log($"[CenterRaycastSpaceApply] Saved: {positionToSave} (Hit: {hitTransform.name}, originMode={saveObjectOrigin})");

            // PlayerPrefs にも保存（JSON）
            SavePositionsToPrefs();

            // 適用対象が設定されていれば適用
            if (applyTarget != null)
            {
                ApplyPosition(applyTarget, positionToSave);
            }
        }
        else
        {
            if (debugLog) Debug.Log("[CenterRaycastSpaceApply] レイは何にも当たりませんでした。");
        }
    }

    /// <summary>
    /// 単一座標を対象に適用（即時 or スムーズ）
    /// </summary>
    private void ApplyPosition(Transform target, Vector3 pos)
    {
        if (smoothApply)
        {
            moveTargetPos = pos;
            isMoving = true;
            if (debugLog) Debug.Log($"[CenterRaycastSpaceApply] （スムーズ）適用開始 pos={pos}");
        }
        else
        {
            if (applyAsWorldPosition) target.position = pos;
            else target.localPosition = pos;
            if (debugLog) Debug.Log($"[CenterRaycastSpaceApply] 適用完了 pos={pos}, asWorld={applyAsWorldPosition}");
        }
    }

    /// <summary>
    /// PlayerPrefs に JSON で保存
    /// </summary>
    public void SavePositionsToPrefs()
    {
        string json = JsonUtility.ToJson(new Vector3ListWrapper(savedPositions));
        PlayerPrefs.SetString("saved_positions", json);
        PlayerPrefs.Save();

        if (debugLog) Debug.Log($"[CenterRaycastSpaceApply] PlayerPrefs に {savedPositions.Count} 件保存しました。");
    }

    /// <summary>
    /// PlayerPrefs から読み込み
    /// </summary>
    public void LoadPositionsFromPrefs()
    {
        string json = PlayerPrefs.GetString("saved_positions", string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<Vector3ListWrapper>(json);
            savedPositions = wrapper?.positions ?? new List<Vector3>();
            if (debugLog) Debug.Log($"[CenterRaycastSpaceApply] PlayerPrefs から {savedPositions.Count} 件読み込みました。");
        }
        else
        {
            if (debugLog) Debug.Log("[CenterRaycastSpaceApply] PlayerPrefs に保存はありません。");
        }
    }

    /// <summary>
    /// リストと PlayerPrefs をクリア
    /// </summary>
    public void ClearSavedPositions(bool alsoClearPrefs = false)
    {
        savedPositions.Clear();
        if (alsoClearPrefs)
        {
            PlayerPrefs.DeleteKey("saved_positions");
            if (debugLog) Debug.Log("[CenterRaycastSpaceApply] PlayerPrefs をクリアしました。");
        }
    }

    // JsonUtility で List<Vector3> を扱うためのラッパー
    [System.Serializable]
    private class Vector3ListWrapper
    {
        public List<Vector3> positions;
        public Vector3ListWrapper(List<Vector3> list) { positions = list; }
    }
}
