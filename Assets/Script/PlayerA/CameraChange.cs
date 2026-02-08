using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraChange : MonoBehaviour
{
    // 切り替えたいカメラ位置のリスト.
    public List<Vector3> cameraPositions = new();
    public List<Image> cameraImage = new();

    // 現在のインデックス.
    private int currentIndex = 0;

    void Start()
    {
        // 最初の位置へ移動.
        if (cameraPositions.Count > 0)
        {
            transform.position = cameraPositions[0];
        }
        UpdateImageColor(0);
    }

    void Update()
    {
        // キー入力対応.
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) JumpTo(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) JumpTo(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) JumpTo(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) JumpTo(3);
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) JumpTo(4);
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) JumpTo(5);
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) JumpTo(6);
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) JumpTo(7);
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) JumpTo(8);
    }

    // 次の位置へ.
    public void ClickNext()
    {
        JumpTo(currentIndex + 1);
    }

    // 前の位置へ.
    public void ClickPrev()
    {
        JumpTo(currentIndex - 1);
    }

    // 指定インデックスの位置へ移動.
    public void JumpTo(int index)
    {
        if (cameraPositions.Count == 0) return;

        // 範囲ループ.
        if (index < 0) index = cameraPositions.Count - 1;
        if (index >= cameraPositions.Count) index = 0;

        currentIndex = index;

        // カメラの位置を変更.
        transform.position = cameraPositions[currentIndex];
        UpdateImageColor(currentIndex);
    }

    // 現在のカメラ番号の色を変更.
    private void UpdateImageColor(int activeIndex)
    {
        for (int i = 0; i < cameraImage.Count; i++)
        {
            if (cameraImage[i] == null) continue;

            if (i == activeIndex)
            {
                // 選択中の画像の色.
                cameraImage[i].color = Color.white;
            }
            else
            {
                // 非選択の画像の色.
                cameraImage[i].color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }
}