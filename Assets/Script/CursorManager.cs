
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    void Awake()
    {
        // カーソルを表示
        Cursor.visible = true;

        // ロック解除（ウィンドウから出てもOKにするなら None）
        Cursor.lockState = CursorLockMode.None;
    }

    // 必要なら、Escで表示を復帰するショートカット
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
