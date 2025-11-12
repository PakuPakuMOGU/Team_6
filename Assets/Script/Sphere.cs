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
    }

    public WinAElements winA;
    public WinBElements winB;
    public bool winATag = true;

    private bool finish = false;

    void Start()
    {
        winA.SetActiveAll(false);
        winB.SetActiveAll(false);
        // 設置時の処理は任せます.
        // if(winATag) winA.SetActiveAll(true);
        // else winB.SetActiveAll(true);
    }

    void Update()
    {

    }

    public void OnTriggerEnter(Collider other)
    {
        // ダメージタグへの接触.
        if (other.gameObject.tag == "Damage" && !finish)
        {
            finish = true;
            GameFinish();
        }
    }

    private void GameFinish()
    {
        // ゲーム終了時の内容を記載.
        if (winATag)
        {
            winA.SetActiveAll(true);
        }
        else
        {
            winB.SetActiveAll(true);
        }

        // インターネットで通知.
    }
}