using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class See : MonoBehaviour
{
    public GameObject cam;
    public float Xsensityvity = 3f; // 上下感度
    public float Ysensityvity = 3f; // 左右感度

    Quaternion cameraRot, characterRot;
    bool cursorLock = true;

    // 上下回転の制限
    float minX = -90f, maxX = 90f;

    void Start()
    {
        cameraRot = cam.transform.localRotation;
        characterRot = transform.localRotation;
    }

    void Update()
    {
        float xRot = Input.GetAxis("Mouse X") * Ysensityvity;
        float yRot = Input.GetAxis("Mouse Y") * Xsensityvity;

        // 左右：プレイヤーの体を回す
        characterRot *= Quaternion.Euler(0f, xRot, 0f);

        // 上下：カメラのX軸（縦）を回す
        cameraRot *= Quaternion.Euler(-yRot, 0f, 0f);
        cameraRot = ClampRotation(cameraRot); // ここで角度制限

        // 回転を反映
        transform.localRotation = characterRot;
        cam.transform.localRotation = cameraRot;

        UpdateCursorLock();
    }

    void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLock = false;
        }
        else if (Input.GetMouseButton(0))
        {
            cursorLock = true;
        }

        Cursor.lockState = cursorLock ? CursorLockMode.Locked : CursorLockMode.None;
    }

    // 上下の回転を制限する関数
    Quaternion ClampRotation(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = Mathf.Atan(q.x) * Mathf.Rad2Deg * 2.0f;
        angleX = Mathf.Clamp(angleX, minX, maxX);
        q.x = Mathf.Tan(angleX * Mathf.Deg2Rad * 0.5f);

        return q;
    }
}