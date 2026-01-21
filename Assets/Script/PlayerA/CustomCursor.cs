
using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [SerializeField] Texture2D cursorTex;   // 32x32 などのテクスチャ
    [SerializeField] Vector2 hotSpot = new Vector2(0, 0); // クリック判定点（左上なら 0,0 / 画像中央なら半分に）

    void Start()
    {
        Cursor.SetCursor(cursorTex, hotSpot, CursorMode.Auto);
        Cursor.visible = true;  // 表示（非表示にしたい時は false）
        Cursor.lockState = CursorLockMode.None;
    }
}
