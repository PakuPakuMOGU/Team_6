using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Point : MonoBehaviour
{
    private int point = 0;

    public bool Add(int add)
    {
        if(point < -add) return false;

        this.point += add;
        return true;
    }
}
