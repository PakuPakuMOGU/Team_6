using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public float speed = 6f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;

    public Transform viewPivot; // 視点の横回転（ローカル専用）
    public Transform head;      // 上下回転
    public Transform body;      // 見た目の体（同期用）

    float pitch = 0f;
    float yaw = 0f;

    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    private bool cursorLock = true;

    private NetworkCharacterController ncc;
    public Animator animator;

    [Networked] private Quaternion NetRotation { get; set; }

    public override void Spawned()
    {
        ncc = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();

        if (Object.HasStateAuthority)
        {
            NetRotation = transform.rotation;
        }
    }

    void Start()
    {
        animator.SetBool("Attack", false);
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        UpdateCursorLock();

        float mouseX = Input.GetAxisRaw("Mouse X") * Xsensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Ysensitivity;

        // 視点の横回転.
        yaw += mouseX;
        viewPivot.localRotation = Quaternion.Euler(0, yaw, 0);

        // 上下回転.
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.localRotation = Quaternion.Euler(pitch, 0, 0);

        // 体は補間して追従させて回転.
        body.localRotation = Quaternion.Slerp(
            body.localRotation,
            Quaternion.Euler(-90, yaw, 90),
            Time.deltaTime * 10f);
    }
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return;

        // 移動.
        if (Object.HasStateAuthority)
        {
            Vector3 move = viewPivot.TransformDirection(new Vector3(data.direction.x, 0, data.direction.y));
            move *= speed;
            ncc.Move(move);

            // ジャンプ.
            if (data.jumpPressed)
                ncc.Jump(data.jumpPressed);
        }

        // アニメーション.
        if (animator != null)
            animator.SetFloat("speed", data.direction.magnitude);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetRotation(Quaternion rot)
    {
        NetRotation = rot;
    }

    void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) cursorLock = false;
        else if (Input.GetMouseButton(0)) cursorLock = true;
    }
}