
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SoftwareCursor : MonoBehaviour
{
    [SerializeField] private RectTransform cursorRect; // カーソル画像の RectTransform
    [SerializeField] private UnityEngine.Camera uiCamera;

    void Update()
    {
        // 実マウスで動かす場合（戦略A/B）
        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        // 画面座標 → Canvas座標（Screen Space - Camera）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, pos, uiCamera, out var localPoint);

        cursorRect.anchoredPosition = localPoint;
    }
}
