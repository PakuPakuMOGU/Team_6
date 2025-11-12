using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sphere : MonoBehaviour
{
    public GameObject SphereObj;
    public Image BackImage;

    [System.Serializable]
    public class WinAElements
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
            if (image != null)
            {
                RectTransform rt = image.rectTransform;
                rt.localScale = new Vector3(1, 0, 1);
                host.StartCoroutine(AnimateScale(rt, duration));
            }
        }

        private IEnumerator AnimateScale(RectTransform rt, float duration)
        {
            float time = 0f;
            Vector3 start = new Vector3(1, 0, 1);
            Vector3 end = new Vector3(1, 1, 1);

            while (time < duration)
            {
                rt.localScale = Vector3.Lerp(start, end, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            rt.localScale = end;
        }
    }

    [System.Serializable]
    public class WinBElements
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
            if (image != null)
            {
                RectTransform rt = image.rectTransform;
                rt.localScale = new Vector3(1, 0, 1);
                host.StartCoroutine(AnimateScale(rt, duration));
            }
        }

        private IEnumerator AnimateScale(RectTransform rt, float duration)
        {
            float time = 0f;
            Vector3 start = new Vector3(1, 0, 1);
            Vector3 end = new Vector3(1, 1, 1);

            while (time < duration)
            {
                rt.localScale = Vector3.Lerp(start, end, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            rt.localScale = end;
        }
    }

    public WinAElements winA;
    public WinBElements winB;
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
        if (winATag)
        {
            winA.SetActiveAll(true);
            winA.AnimateOpen(0.3f, this); // ← アニメーション追加
        }
        else
        {
            winB.SetActiveAll(true);
            winB.AnimateOpen(0.3f, this); // ← アニメーション追加
        }

        // インターネットで通知.
    }
}