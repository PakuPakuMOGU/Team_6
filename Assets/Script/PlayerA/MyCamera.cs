using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Camera : MonoBehaviour
{
    public GameObject cam;
    public float Xsensityvity = 3f; // 上下感度
    public float Ysensityvity = 3f; // 左右感度

    Quaternion cameraRot, characterRot;

    float xRotation = 0f; // カメラの上下角
    float yRotation = 0f; // プレイヤーの左右角

    // 上下回転の制限
    float minX = -90f, maxX = 90f;

    void Start()
    {
        // 現在の感度をロード
        Xsensityvity = PlayerPrefs.GetFloat("X_Sensitivity", Xsensityvity);
        Ysensityvity = PlayerPrefs.GetFloat("Y_Sensitivity", Ysensityvity);

        // 現在の回転を記録して、初期向きを維持
        Vector3 camAngles = cam.transform.localEulerAngles;
        Vector3 charAngles = transform.localEulerAngles;

        xRotation = camAngles.x;
        yRotation = charAngles.y;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * Ysensityvity;
        float mouseY = Input.GetAxis("Mouse Y") * Xsensityvity;

        // 角度を更新
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minX, maxX);

        // 回転を反映
        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

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

    public void SetX(float value)
    {
        Xsensityvity = value;
        PlayerPrefs.SetFloat("X_Sensitivity", value);
    }

    public void SetY(float value)
    {
        Ysensityvity = value;
        PlayerPrefs.SetFloat("Y_Sensitivity", value);
    }

    void OnDisable()
    {
        PlayerPrefs.Save(); // シーン遷移や設定画面閉じた時に保存
    }
}