
using UnityEngine;

public class UIButtonNudge : MonoBehaviour
{
    public Transform target;   // 動かしたいオブジェクト
    public float step = 0.1f;
    public bool useLocalSpace = false;

    public float rotationStepDegrees = 20f; // 回転の角度（度）

    public Shop_Maneger shop;
    public int targetIndex;

    // 位置の微調整
    public void NudgeUp() => Nudge(new Vector3(0f, 6f, 0f));
    public void NudgeDown() => Nudge(new Vector3(0f, -6f, 0f));
    public void NudgeLeft() => Nudge(new Vector3(-6f, 0f, 0f));
    public void NudgeRight() => Nudge(new Vector3(6f, 0f, 0f));

    // 回転の微調整（Z軸のみ）
    public void RotateClockwise() => Rotate(rotationStepDegrees);
    public void RotateCounterClockwise() => Rotate(-rotationStepDegrees);

    void Update()
    {
        targetIndex = shop.targetIndex;
        target = shop.targets[targetIndex];
    }

    private void Nudge(Vector3 dir)
    {
        if (target == null) return;

        Vector3 delta = dir * step;
        if (useLocalSpace) target.localPosition += delta;
        else target.position += delta;
    }

    private void Rotate(float degrees)
    {
        if (target == null) return;

        var space = useLocalSpace ? Space.Self : Space.World;
        target.Rotate(Vector3.up, degrees, space);
    }
}
