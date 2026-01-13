using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;


public class PlayerController : NetworkBehaviour
{
    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public GameObject cam;
    public float Xsensityvity = 3f;
    public float Ysensityvity = 3f;

    private float xRotation = 0f;
    private bool cursorLock = true;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>(); // Animatorを取得

        if (!Object.HasInputAuthority)
        {
            cam.SetActive(false);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        UpdateCursorLock();

        float mouseY = Input.GetAxis("Mouse Y") * Ysensityvity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !GetInput(out NetworkInputData data)) return;

        // プレイヤーごとY軸回転.
        float newY = transform.eulerAngles.y + data.rotation * Xsensityvity;
        transform.rotation = Quaternion.Euler(0, newY, 0);

        // カメラの上下回転.
        Vector3 input = new Vector3(data.direction.x, 0.0f, data.direction.y);
        Vector3 horizontalMove = (transform.forward * input.z + transform.right * input.x) * speed;

        // 水平方向だけ更新.
        Vector3 move = new Vector3(horizontalMove.x, moveDirection.y, horizontalMove.z);
        moveDirection = move;

        if (controller != null && controller.isGrounded)
        {
            moveDirection.y = data.jumpPressed ? jumpSpeed : 0f;
        }

        moveDirection.y -= gravity * Runner.DeltaTime;

        if (controller != null)
            controller.Move(moveDirection * Runner.DeltaTime);

        if (animator != null)
            animator.SetFloat("speed", input.magnitude);
    }

    void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) cursorLock = false;
        else if (Input.GetMouseButton(0)) cursorLock = true;

        Cursor.lockState = cursorLock ? CursorLockMode.Locked : CursorLockMode.None;
    }
}