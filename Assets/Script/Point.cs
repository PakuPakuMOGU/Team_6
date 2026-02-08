using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Point : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private AudioSource Sound;
    private int point = 0;

    void Start()
    {
        textMeshPro.text = point.ToString();
    }

    public bool Add(int add)
    {
        if(point < -add) return false;

        this.point += add;
        textMeshPro.text = point.ToString();
        Sound.Play();
        return true;
    }
}