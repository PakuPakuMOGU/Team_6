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
        if (!Object.HasInputAuthority) return;

        float mouseX = Input.GetAxis("Mouse X") * Xsensityvity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * Ysensityvity;

        transform.Rotate(Vector3.up * mouseX);

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