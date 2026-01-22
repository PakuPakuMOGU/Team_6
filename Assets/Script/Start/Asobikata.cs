using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Asobikata : MonoBehaviour
{
    public List<Image> images = new();
    private int num = 0;

    void Start()
    {
        // 最初の画像のみ表示.
        for (int i = 0; i < images.Count; i++)
            images[i].gameObject.SetActive(i == 0);
    }

    // 次のページへ.
    public void ClickNext()
    {
        JumpTo(num + 1);
    }

    // 前のページへ.
    public void ClickPrev()
    {
        JumpTo(num - 1);
    }

    // 指定ページへジャンプ.
    public void JumpTo(int index)
    {
        if (index < 0) index = images.Count - 1;
        if (index >= images.Count) index = 0;

        // 現在のページを非表示.
        images[num].gameObject.SetActive(false);

        // 新しいページを表示.
        num = index;
        images[num].gameObject.SetActive(true);
    }

    // 遊び方を閉じる.
    public void close()
    {
        images[num].gameObject.SetActive(false);
    }
}