using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class View : MonoBehaviour
{
    public Image BackImage;
    public Image image;
    public Image string1;
    public Image string2;

    // 必要な画像を表示.
    public void SetActiveAll(bool isActive)
    {
        if (image   != null) image.gameObject.SetActive(isActive);
        if (string1 != null) string1.gameObject.SetActive(isActive);
        if (string2 != null) string2.gameObject.SetActive(isActive);
    }

    // ウィンドウを上下に開いて表示する数値設定.
    public void AnimateOpen(float duration, MonoBehaviour host)
    {
        SetActiveAll(true);

        AnimateOne(image, duration, 0f, host);
        AnimateOne(string1, duration - 0.7f, 0.8f, host);
        AnimateOne(string2, duration - 0.7f, 1.0f, host);
    }

    // ウィンドウを上下に開いて表示.
    private void AnimateOne(Image target, float duration, float delay, MonoBehaviour host)
    {
        if (target == null) return;
        RectTransform rt = target.rectTransform;
        rt.localScale = new Vector3(150, 0, 1);
        host.StartCoroutine(AnimateScaleWithDelay(rt, duration, delay));
    }

    // 一拍おきたいとき用.
    private IEnumerator AnimateScaleWithDelay(RectTransform rt, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return AnimateScale(rt, duration);
    }

    private IEnumerator AnimateScale(RectTransform rt, float duration)
    {
        // スタート時のサイズ、終了時のサイズを設定.
        float time = 0f;
        Vector3 start = new Vector3(150, 0, 1);
        Vector3 end = new Vector3(150, 150, 1);

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

    public void WindowView()
    {
        SetActiveAll(true);
        AnimateOpen(0.8f, this);
    }

    public void WindowClose()
    {
        SetActiveAll(false);
    }
}
