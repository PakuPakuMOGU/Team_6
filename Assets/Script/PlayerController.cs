using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public float speed = 6f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;

    public GameObject cam;
    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    private float xRotation = 0f;
    private bool cursorLock = true;

    private NetworkCharacterController ncc;
    private Animator animator;

    [Networked] private Quaternion NetRotation { get; set; }

    public override void Spawned()
    {
        ncc = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();

        if (!Object.HasInputAuthority)
        {
            cam.SetActive(false);
        }

        if (Object.HasStateAuthority)
        {
            NetRotation = transform.rotation;
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        UpdateCursorLock();

        // カメラ上下回転（ローカルのみ）
        float mouseY = Input.GetAxis("Mouse Y") * Ysensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return;

        // 回転同期（あなたのRPC方式）
        if (Object.HasInputAuthority)
        {
            float newY = NetRotation.eulerAngles.y + data.rotation;
            RPC_SetRotation(Quaternion.Euler(0, newY, 0));
        }

        transform.rotation = NetRotation;

        // 移動ベクトル
        Vector3 move = new Vector3(data.direction.x, 0, data.direction.y);
        move = transform.TransformDirection(move) * speed;

        // --- 予測移動（クライアント） ---
        if (Object.HasInputAuthority)
            ncc.Move(move);

        // --- 正式移動（StateAuthority） ---
        if (Object.HasStateAuthority)
            ncc.Move(move);

        // ジャンプ
        ncc.Jump(data.jumpPressed);
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

        Cursor.lockState = cursorLock ? CursorLockMode.Locked : CursorLockMode.None;
    }
}