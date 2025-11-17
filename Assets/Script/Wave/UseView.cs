using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseView : MonoBehaviour
{
    [SerializeField] private View view;
    void Start()
    {
        view.WindowView();
    }
    public void Close()
    {
        view.WindowClose();
    }
}