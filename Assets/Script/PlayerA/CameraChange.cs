using System.Collections.Generic;
using UnityEngine;

public class CameraChange : MonoBehaviour
{
    // 切り替えたいカメラ位置のリスト
    public List<Vector3> cameraPositions = new();

    // 現在のインデックス
    private int currentIndex = 0;

    void Start()
    {
        // 最初の位置へ移動
        if (cameraPositions.Count > 0)
        {
            transform.position = cameraPositions[0];
        }
    }

    // 次の位置へ
    public void ClickNext()
    {
        JumpTo(currentIndex + 1);
    }

    // 前の位置へ
    public void ClickPrev()
    {
        JumpTo(currentIndex - 1);
    }

    // 指定インデックスの位置へ移動
    public void JumpTo(int index)
    {
        if (cameraPositions.Count == 0) return;

        // 範囲ループ
        if (index < 0) index = cameraPositions.Count - 1;
        if (index >= cameraPositions.Count) index = 0;

        currentIndex = index;

        // カメラの位置を変更
        transform.position = cameraPositions[currentIndex];
    }
}