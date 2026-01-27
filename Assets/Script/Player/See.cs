using UnityEngine;
using Fusion;

public class PlayerRotation : NetworkBehaviour
{
    public Transform viewPivot; // 視点の横回転（ローカル専用）
    public Transform head;      // 上下回転
    public Transform body;      // 見た目の体（同期用）

    float pitch = 0f;
    float yaw = 0f;

    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    void Start()
    {
        /*
        if (!Object.HasInputAuthority)
        {
            head.enabled = false;
        }
        */
    }

    void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * Xsensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Ysensitivity;

        // 視点の横回転（ローカル専用）
        yaw += mouseX;
        viewPivot.localRotation = Quaternion.Euler(0, yaw, 0);

        // 上下回転
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.localRotation = Quaternion.Euler(pitch, 0, 0);

        // 見た目の体はゆっくり追従させる（同期用）
        body.rotation = Quaternion.Euler(0, yaw, 0);
    }
}