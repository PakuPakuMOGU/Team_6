using UnityEngine;
using Fusion;

public class PlayerRotation : NetworkBehaviour
{
    public Transform body;   // 左右回転（同期対象）
    public Transform head;   // 上下回転（ローカルのみ）
    public Camera cam;

    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    private float pitch = 0f;

    void Start()
    {
        if (!Object.HasInputAuthority)
        {
            cam.enabled = false;
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * Xsensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Ysensitivity;

        // --- Body（左右） ---
        body.Rotate(Vector3.up * mouseX);

        // --- Head（上下） ---
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}