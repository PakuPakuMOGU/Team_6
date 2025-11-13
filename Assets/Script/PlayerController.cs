using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;


public class PlayerController : NetworkBehaviour
{
    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>(); // AnimatorÇéÊìæ
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;
        if (!GetInput(out NetworkInputData data)) return;

        Vector3 input = new Vector3(data.direction.x, 0.0f, data.direction.y);
        Vector3 horizontalMove = (transform.forward * input.z + transform.right * input.x) * speed;

        // êÖïΩï˚å¸ÇæÇØçXêV
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
}