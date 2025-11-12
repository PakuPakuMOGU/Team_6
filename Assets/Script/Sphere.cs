using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sphere : MonoBehaviour
{
    public GameObject SphereObj;
    public Image BackImage;

    [System.Serializable]
    public class WinElementsBase
    {
        public Image image;
        public Image string1;
        public Image string2;

        public void SetActiveAll(bool isActive)
        {
            if (image != null) image.gameObject.SetActive(isActive);
            if (string1 != null) string1.gameObject.SetActive(isActive);
            if (string2 != null) string2.gameObject.SetActive(isActive);
        }

        public void AnimateOpen(float duration, MonoBehaviour host)
        {
            SetActiveAll(true);

            AnimateOne(image, duration, 0f, host);
            AnimateOne(string1, duration - 0.7f, 0.8f, host);
            AnimateOne(string2, duration - 0.7f, 1.0f, host);
        }

        private void AnimateOne(Image target, float duration, float delay, MonoBehaviour host)
        {
            if (target == null) return;
            RectTransform rt = target.rectTransform;
            rt.localScale = new Vector3(150, 0, 1);
            host.StartCoroutine(AnimateScaleWithDelay(rt, duration, delay));
        }

        private IEnumerator AnimateScaleWithDelay(RectTransform rt, float duration, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return AnimateScale(rt, duration);
        }

        private IEnumerator AnimateScale(RectTransform rt, float duration)
        {
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
    }

    public WinElementsBase winA;
    public WinElementsBase winB;
    public bool winATag = true;

    private bool finish = false;

    void Start()
    {
        winA.SetActiveAll(false);
        winB.SetActiveAll(false);
        GameFinish();
    }

    void Update()
    {

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Damage" && !finish)
        {
            finish = true;
            GameFinish();
        }
    }

    private void GameFinish()
    {
        // インターネット通知入れる.

        if (BackImage != null) BackImage.gameObject.SetActive(true);
        if (winATag)
        {
            winA.AnimateOpen(0.8f, this);       
        }
        else
        {
            winB.AnimateOpen(0.8f, this);
        }
    }
}