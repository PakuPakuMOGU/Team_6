using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class View : MonoBehaviour
{
    public Image BackImage;
    public List<Image> images = new();
    private bool isAnimating = false; // 二重起動防止フラグ.


    // 追加: 親オブジェクトに CanvasGroup をアタッチしてここに参照を入れる
    public CanvasGroup canvasGroup;

    // 必要な画像を表示.
    public void SetActiveAll(bool isActive)
    {
        foreach (var img in images)
        {
            if (img != null) img.gameObject.SetActive(isActive);
        }
        if (BackImage != null) BackImage.gameObject.SetActive(isActive);
    }

    // ウィンドウを上下に開いて表示する数値設定.
    public void AnimateOpen(float duration, MonoBehaviour host)
    {
        SetActiveAll(true);

        DisableAllInteraction(); // 開始時に全ボタン無効化.

        // 最初の画像はすぐ表示.
        if (images.Count > 0)
            AnimateOne(images[0], duration, 0f, host);

        // 2枚目以降は少し遅らせて表示.
        for (int i = 1; i < images.Count; i++)
        {
            AnimateOne(images[i], duration - 0.7f, 0.8f + (i * 0.2f), host);
        }

        // 終了後に有効化
        host.StartCoroutine(EnableInteractionAfter(duration + images.Count * 0.2f));
    }

    // 個別の画像をアニメーション表示.
    private void AnimateOne(Image target, float duration, float delay, MonoBehaviour host)
    {
        if (target == null) return;
        RectTransform rt = target.rectTransform;
        Vector3 originalScale = rt.localScale;
        rt.localScale = new Vector3(originalScale.x, 0, originalScale.z);

        host.StartCoroutine(AnimateScaleWithDelay(rt, duration, delay, originalScale));
    }

    private IEnumerator AnimateScaleWithDelay(RectTransform rt, float duration, float delay, Vector3 originalScale)
    {
        yield return new WaitForSeconds(delay);
        yield return AnimateScale(rt, duration, originalScale);
    }

    private IEnumerator AnimateScale(RectTransform rt, float duration, Vector3 originalScale)
    {
        float time = 0f;
        Vector3 start = new Vector3(originalScale.x, 0, originalScale.z);
        Vector3 end = originalScale;

        while (time < duration)
        {
            rt.localScale = Vector3.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        rt.localScale = end;
    }

    public void Start()
    {
        SetActiveAll(false);
        DisableAllInteraction(); // 初期状態ではクリック不可.
    }

    // ウィンドウを表示.
    public void WindowView()
    {
        if (isAnimating) return; // 連打防止.
        isAnimating = true;

        SetActiveAll(true);
        AnimateOpen(0.8f, this);
    }

    // アニメーション終了後に呼ばれる
    private IEnumerator EnableInteractionAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        EnableAllInteraction();
        isAnimating = false; // アニメーション完了で解除.
    }

    // ウィンドウを閉じる.
    public void WindowClose()
    {
        SetActiveAll(false);
        DisableAllInteraction(); // 閉じたらクリック不可.
    }

    // ★追加: 全部のボタンを無効化
    private void DisableAllInteraction()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;      // ボタンなどの操作を禁止.
            canvasGroup.blocksRaycasts = false;    // クリック判定を遮断.
        }
    }

    // ★追加: 全部のボタンを有効化
    private void EnableAllInteraction()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;       // ボタン操作を許可
            canvasGroup.blocksRaycasts = true;     // クリック判定を復活
        }
    }
}