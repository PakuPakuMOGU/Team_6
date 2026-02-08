using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Point : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private int SphereNum = 19;
    [SerializeField] private AudioSource Sound;
    [SerializeField] private Wave wave;
    private int point = 0;

    void Start()
    {
        textMeshPro.text = point.ToString();
    }

    // ポイント追加.
    public bool Add(int add)
    {
        if(point < -add) return false;

        this.point += add;
        textMeshPro.text = point.ToString();
        Sound.Play();
        return true;
    }

    // スフィアの個数把握.
    public void SphereBreak()
    {
        SphereNum--;

        // すべて壊したらクリア.
        if (SphereNum <= 0)
            wave.EndGame_B();
    }
}