using UnityEngine;

public class PurchaseButton : MonoBehaviour
{
    

    [Header("Purchase Settings")]
    public GameObject prefabToPlace; // 購入して設置したいプレハブ
    public GameObject shopWindow;    // 購入後に閉じたいUI（Panelなど）

    // UI Button の OnClick に登録する
    public void OnClickPurchase()
    {
        if (shopWindow) shopWindow.SetActive(false); // ウィンドウを閉じる
        PlacementManager.Instance.StartPlacement(prefabToPlace);
    }
}