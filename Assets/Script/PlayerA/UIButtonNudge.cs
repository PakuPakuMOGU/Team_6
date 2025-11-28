
using UnityEngine;

public class UIButtonNudge : MonoBehaviour
{
    public Transform target;   // 動かしたいオブジェクト
    public float step = 0.1f;
    public bool useLocalSpace = false;

    public Shop_Maneger shop;
    public int targetIndex;

    public void NudgeUp() => Nudge(new Vector3(0f, 2f, 0f));
    public void NudgeDown() => Nudge(new Vector3(0f, -2f, 0f));
    public void NudgeLeft() => Nudge(new Vector3(-2f, 0f, 0f));
    public void NudgeRight() => Nudge(new Vector3(2f, 0f, 0f));

    void Update()
    {
        targetIndex = shop.targetIndex;
        target = shop.targets[targetIndex];
        
    }


    private void Nudge(Vector3 dir)
    {
        Vector3 delta = dir * step;
        if (useLocalSpace) target.localPosition += delta;
        else target.position += delta;
    }
}