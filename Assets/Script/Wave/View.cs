using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class View : MonoBehaviour
{
    public Image BackImage;
    public List<Image> images = new();

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

        // 最初の画像はすぐ表示.
        if (images.Count > 0)
            AnimateOne(images[0], duration, 0f, host);

        // 2枚目以降は少し遅らせて表示.
        for (int i = 1; i < images.Count; i++)
        {
            AnimateOne(images[i], duration - 0.7f, 0.8f + (i * 0.2f), host);
        }
    }

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
    }

    // ウィンドウを表示.
    public void WindowView()
    {
        SetActiveAll(true);
        AnimateOpen(0.8f, this);
    }

    // ウィンドウを閉じる.
    public void WindowClose()
    {
        SetActiveAll(false);
    }
}