
using UnityEngine;

public class UIButtonNudge : MonoBehaviour
{
    [Header("参照")]
    public Shop_Maneger shop;       // 必須：マネージャをアサイン
    public Transform overrideTarget; // 任意：手動で直接対象を指定したい場合

    [Header("移動ステップ")]
    [Tooltip("1クリックあたりの移動量（メートル）")]
    public float moveStep = 4f;

    [Tooltip("ローカル空間で動かすなら true、ワールドなら false")]
    public bool useLocalSpace =false ;

    [Header("回転ステップ")]
    [Tooltip("1クリックあたりの回転角度（度）")]
    float rotationStepDegrees = 12f;

    [Tooltip("回転軸（通常はY軸で水平回転）")]
    public Axis rotateAxis = Axis.Y;
    public enum Axis { X, Y, Z }

    // 上下左右（＋前後）が必要ならUI側でボタンに割り当て
    public void NudgeUp() => Nudge(new Vector3(0f, +30f, 0f));
    public void NudgeDown() => Nudge(new Vector3(0f, -30f, 0f));
    public void NudgeLeft() => Nudge(new Vector3(-30f, 0f, 0f));
    public void NudgeRight() => Nudge(new Vector3(+30f, 0f, 0f));
    public void NudgeForward() => Nudge(new Vector3(0f, 0f, +30f));
    public void NudgeBack() => Nudge(new Vector3(0f, 0f, -30f));

    public void RotateClockwise() => Rotate(+rotationStepDegrees);
    public void RotateCounterClockwise() => Rotate(-rotationStepDegrees);

    // -----------------------------
    // 内部：対象の解決と適用
    // -----------------------------
    private Transform ResolveTarget()
    {
        // 手動指定があればそれを優先
        if (overrideTarget != null) return overrideTarget;

        // マネージャの現在対象を使う（推奨）
        if (shop != null && shop.CurrentTarget != null) return shop.CurrentTarget;

        Debug.LogWarning("[UIButtonNudge] 対象がありません。直前に設置した後、編集UIを閉じていないか確認してください。");
        return null;
    }

    private void Nudge(Vector3 dirUnit)
    {
        var t = ResolveTarget();
        if (t == null) return;

        shop.RPC_Nudge(dirUnit, moveStep, useLocalSpace);
    }

    private void Rotate(float degrees)
    {
        var t = ResolveTarget();
        if (t == null) return;

        shop.RPC_Rotate(degrees, rotateAxis, useLocalSpace);
    }
}
