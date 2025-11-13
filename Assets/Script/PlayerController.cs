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
        animator = GetComponent<Animator>(); // Animator‚ðŽæ“¾
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;

        Vector3 input = new Vector3(data.direction.x, 0, data.direction.y);

        if (controller.isGrounded)
        {
            Vector3 horizontalMove = transform.TransformDirection(input) * speed;
            moveDirection.x = horizontalMove.x;
            moveDirection.z = horizontalMove.z;

            moveDirection.y = data.jumpPressed ? jumpSpeed : 0f;
        }
        else
        {
            Vector3 horizontalMove = transform.TransformDirection(input) * speed;
            moveDirection.x = horizontalMove.x;
            moveDirection.z = horizontalMove.z;
        }

        moveDirection.y -= gravity * Runner.DeltaTime;
        controller.Move(moveDirection * Runner.DeltaTime);

        animator.SetFloat("speed", input.magnitude);
    }
}