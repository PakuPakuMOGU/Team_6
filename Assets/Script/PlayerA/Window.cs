using UnityEngine;
using UnityEngine.EventSystems;

public class Window : MonoBehaviour
{

    public bool WindowOpen;
    public GameObject image;
    public float moveDistance = 100f;   // 上に移動する距離
    public float moveSpeed = 5f;        // 移動速度

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    void Start()
    {
        WindowOpen = false;
        originalPosition = image.transform.localPosition; // 初期位置を記録
        targetPosition = originalPosition;                // 初期ターゲットも同じ
    }

    void Update()
    {
        // 左クリックされたか判定
        if (Input.GetMouseButtonDown(0))
        {
            // UIボタンが押されたか判定
            if (EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.CompareTag("UIButton"))
            {
                ToggleWindow(); // ← ボタン押下時に処理を呼ぶ
            }
        }

        // スムーズに移動
        image.transform.localPosition = Vector3.Lerp(image.transform.localPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void ToggleWindow()
    {
        WindowOpen = !WindowOpen;

        if (WindowOpen)
        {
            image.SetActive(true);
            targetPosition = originalPosition + new Vector3(0, moveDistance, 0); // 上に移動
            Debug.Log("ウィンドウを開きました");
        }
        else
        {
            targetPosition = originalPosition; // 元の位置に戻す
            Debug.Log("ウィンドウを閉じました");
        }
    }
}