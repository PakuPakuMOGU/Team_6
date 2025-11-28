
using UnityEngine;

public class Shop : MonoBehaviour
{
    public Window window;
    public Shop_Maneger Maneger;
    public GameObject Window_Button;

    // ボタンの OnClick にこのメソッドを割り当て
    public void OnButtonClick()  
    {   
        window.ToggleWindow();
        //Window_Button.SetActive(false);

        // ボタン自身のタグを取得
        string tagName = gameObject.tag;

        // ウィンドウの開閉（必要に応じて）
         

        // マネージャへタグ名で設置依頼
        if (Maneger != null) Maneger.BuyByTag(tagName);
        else Debug.LogWarning("Shop.Maneger が未設定です");
    }
}