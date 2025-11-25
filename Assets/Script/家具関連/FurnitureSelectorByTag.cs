
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FurnitureSelectorByTag : MonoBehaviour
{
    [Header("既存の設置コントローラ")]
    public RuntimePlacementController placement;

    [Header("タグ→Prefab の対応表")]
    public List<TagPrefabPair> tagPrefabs = new List<TagPrefabPair>();

    [Header("選択時に設置モードを自動ON")]
    public bool autoEnterPlaceMode = true;

    // ★ UIボタンの OnClick にこのメソッドを割り当ててください（引数なし）
    public void SelectByButtonTag()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("EventSystem がシーンにありません。UIイベントが取得できません。");
            return;
        }

        GameObject clicked = EventSystem.current.currentSelectedGameObject;
        if (clicked == null)
        {
            Debug.LogWarning("クリック元の GameObject を取得できませんでした。");
            return;
        }

        // UI構造によっては子（Text/Image）が選択されることがあるので親Buttonを優先
        var btn = clicked.GetComponentInParent<Button>();
        GameObject buttonObject = btn ? btn.gameObject : clicked;

        string tag = buttonObject.tag;
        if (string.IsNullOrEmpty(tag) || tag == "Untagged")
        {
            Debug.LogWarning($"Button '{buttonObject.name}' にタグが設定されていません（Untagged）。タグを付けてください。");
            return;
        }

        // 対応するPrefabを探す
        var prefab = FindPrefabByTag(tag);
        if (prefab == null)
        {
            Debug.LogWarning($"タグ '{tag}' に対応する Prefab が見つかりません。TagPrefabPair の設定を確認してください。");
            return;
        }

        if (placement == null)
        {
            Debug.LogWarning("placement が未設定です。RuntimePlacementController を割り当ててください。");
            return;
        }

        // 設置対象を切り替え
        placement.furniturePrefab = prefab;

        // 設置モード中ならゴースト差し替え
        placement.RefreshGhostWithCurrentPrefab();

        // 自動で設置モードON（すでにONなら維持／OFFならONに）
        if (autoEnterPlaceMode)
        {
            placement.SetPlaceMode(true);
        }

        Debug.Log($"選択: Tag '{tag}' → Prefab '{prefab.name}'");
    }

    private GameObject FindPrefabByTag(string tag)
    {
        foreach (var p in tagPrefabs)
        {
            if (p != null && p.tag == tag) return p.prefab;
        }
        return null;
    }
}
