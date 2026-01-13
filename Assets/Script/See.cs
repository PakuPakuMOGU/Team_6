using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class See : NetworkBehaviour
{
    public GameObject cam;
    public float Xsensityvity = 3f;
    public float Ysensityvity = 3f;
    private Animator animator;
    private CharacterController controller;

    float xRotation = 1f;
    bool cursorLock = true;

    private NetworkObject netObj;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.HasInputAuthority)
        {
            cam.SetActive(false);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        UpdateCursorLock();

        if (!GetInput(out NetworkInputData data)) return;

        // 多分ここが死んでる.クライアント側だけプレイヤーが横回転してない.
        // ちがう、最初に入室したプレイヤー以外？
        // そもそも最初に入室したらホストだった.
        // プレイヤー自体を回転.
        //transform.Rotate(0, data.rotation * Xsensityvity * Time.deltaTime, 0);
        float newY = transform.eulerAngles.y + data.rotation * Xsensityvity * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, newY, 0);
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(rotation: transform.rotation); // 強制同期
        }

        // カメラのみを回転（マウスYはローカルで処理）
        float mouseY = Input.GetAxis("Mouse Y") * Ysensityvity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) cursorLock = false;
        else if (Input.GetMouseButton(0)) cursorLock = true;

        Cursor.lockState = cursorLock ? CursorLockMode.Locked : CursorLockMode.None;
    }
}