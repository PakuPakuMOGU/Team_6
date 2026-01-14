using UnityEngine;
using Fusion;

public class PlayerRotation : NetworkBehaviour
{
    public GameObject cam;
    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    private float xRotation = 0f;

    // ネットワーク同期される回転.
    [Networked] private Quaternion NetRotation { get; set; }

    void Start()
    {
        // 自分以外のプレイヤーはカメラを無効化.
        if (!Object.HasInputAuthority)
        {
            cam.SetActive(false);
        }
    }

    void Update()
    {
        // 自分のキャラだけ入力を処理.
        if (!Object.HasInputAuthority)
        {
            // 他人の回転は NetRotation を適用.
            transform.rotation = NetRotation;
            return;
        }

        // --- マウス入力 ---
        float mouseX = Input.GetAxis("Mouse X") * Xsensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * Ysensitivity;

        // 水平回転（プレイヤー本体）.
        float newY = transform.eulerAngles.y + mouseX;
        Quaternion newRot = Quaternion.Euler(0, newY, 0);

        // RPC で StateAuthority に送る.
        RPC_SetRotation(newRot);

        // 垂直回転（カメラのみ）.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    // 回転を確定して全員に同期.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetRotation(Quaternion rot)
    {
        NetRotation = rot;
    }

    public override void FixedUpdateNetwork()
    {
        // 全員がNetRotationを適用.
        transform.rotation = NetRotation;
    }
}