using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;


public class PlayerController : NetworkBehaviour
{
    [Header("Move Settings")]
    public float speed = 6f;

    [Header("Look Settings")]
    public GameObject cam;
    public float Xsensitivity = 3f;
    public float Ysensitivity = 3f;

    private View menuView;
    private SetActiveOnly setAc;

    [Header("Jump Limit Settings (No IsGrounded)")]
    [Tooltip("ジャンプのクールダウン（秒）")]
    public float jumpCooldown = 0.15f;
    [Tooltip("ジャンプ入力を先行受付する時間（秒）")]
    public float jumpBufferTime = 0.1f;
    [Tooltip("空中ジャンプ可能回数（0 = 空中ジャンプなし / 地上1回のみ）")]
    public int maxAirJumps = 0;
    [Tooltip("着地とみなすためのY変化許容（メートル）")]
    public float yEpsilon = 0.01f;
    [Tooltip("Yが安定していたら着地とみなすまでの時間（秒）")]
    public float landedMinStableTime = 0.08f;

    [Header("Jump Tuning")]
    [Tooltip("ジャンプ上向き初速（m/s）。大きいほど高く跳ぶ")]
    public float jumpVelocity = 8f;
    [Tooltip("空中ジャンプ時の倍率（1 段目の jumpVelocity に対する比率）")]
    [Range(0.1f, 2f)]
    public float airJumpMultiplier = 0.8f;

    public GameObject MenuPre;

    private float xRotation = 0f;
    private bool cursorLock = true;

    private float accumulatedYaw = 0f;
    private NetworkCharacterController ncc;
    private Animator animator;

    [Networked] private Quaternion NetRotation { get; set; }

    // ---- Jump control state (StateAuthorityで更新) ----
    [Networked] private float NextJumpAllowedTime { get; set; }
    [Networked] private float LastJumpBufferTime { get; set; }
    [Networked] private int JumpsSinceTakeoff { get; set; } // 離陸後ジャンプ回数（1回目の地上ジャンプを含む）

    // 擬似着地のためのローカル状態（StateAuthorityのみ使用）
    private float lastY;
    private float yStableTime;
    private bool landed;

    private void Awake()
    {
        // メニュー関連のスクリプトを読み込む.
        if (menuView == null)
            menuView = GameObject.Find("MenuView").GetComponent<View>();

        if (setAc == null)
            setAc = FindObjectOfType<SetActiveOnly>();
    }

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

            float now = (float)Runner.SimulationTime;
            NextJumpAllowedTime = now;
            LastJumpBufferTime = -999f;

            // 擬似着地 初期化
            lastY = transform.position.y;
            yStableTime = 0f;
            landed = true;          // スポーン時は着地扱い
            JumpsSinceTakeoff = 0;  // 離陸前
        }
    }

    void Start()
    {
        if (animator != null)
            animator.SetBool("Attack", false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLock = true;
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        UpdateCursorLock();

        // カメラ上下回転（ピッチ）
        float mouseY = Input.GetAxis("Mouse Y") * Ysensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        menuView.WindowView();
        setAc.OpenAfter(1f);
        cursorLock = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return;

        // StateAuthority が回転を適用
        if (Object.HasStateAuthority)
        {
            transform.rotation = Quaternion.Euler(0, data.yaw, 0);
        }

        if (Object.HasStateAuthority)
        {
            float now = (float)Runner.SimulationTime;

            // ---- 移動（Transform に依存しない方向変換）----
            Vector3 localInput = new Vector3(data.direction.x, 0f, data.direction.y);
            Quaternion yaw = Quaternion.Euler(0f, data.yaw, 0f);
            Vector3 worldMove = yaw * localInput * speed;
            ncc.Move(worldMove);

            // ---- 擬似着地検出（Yの安定時間で判定）----
            float currentY = transform.position.y;
            float dy = Mathf.Abs(currentY - lastY);

            if (dy < yEpsilon)
            {
                yStableTime += Runner.DeltaTime; // シミュレーション時間で積算
                if (!landed && yStableTime >= landedMinStableTime)
                {
                    // 着地したとみなす
                    landed = true;
                    JumpsSinceTakeoff = 0; // 着地でリセット
                }
            }
            else
            {
                // 明確に上下している → 空中とみなす
                yStableTime = 0f;
                landed = false;
            }

            lastY = currentY;

            // ---- ジャンプ入力のバッファ ----
            if (data.jumpPressed)
            {
                LastJumpBufferTime = now;
            }

            // ---- ジャンプ可否判定（IsGroundedなし）----
            bool buffered = (now - LastJumpBufferTime) <= jumpBufferTime;
            bool cooldownOK = now >= NextJumpAllowedTime;

            // “離陸後の最大回数” = 1（地上） + maxAirJumps（空中）
            int maxJumpsPerAirTime = 1 + Mathf.Max(0, maxAirJumps);
            bool jumpsLeft = (JumpsSinceTakeoff < maxJumpsPerAirTime);

            if (buffered && cooldownOK && jumpsLeft)
            {
                // 実行（NCCのジャンプ処理を起動）
                ncc.Jump(true);

                // ★数値でジャンプの大きさを制御：上向き初速を上書き
                float appliedVelocity = jumpVelocity;
                bool firstJump = (JumpsSinceTakeoff == 0);
                if (!firstJump)
                    appliedVelocity *= airJumpMultiplier;

                var v = ncc.Velocity;
                v.y = appliedVelocity;
                ncc.Velocity = v;

                // クールダウン更新.
                NextJumpAllowedTime = now + jumpCooldown;

                // バッファ消費.
                LastJumpBufferTime = -999f;

                // 離陸状態に遷移.
                landed = false;
                yStableTime = 0f;
                JumpsSinceTakeoff++;
            }
        }

        // アニメーション.
        if (animator != null)
        {
            float spd = data.direction.magnitude;
            animator.SetFloat("speed", spd, 0.1f, Runner.DeltaTime);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetRotation(Quaternion rot)
    {
        NetRotation = rot;
    }

    // カーソルの非表示化、固定化.
    void UpdateCursorLock()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // UI をクリックしている → ロックしない
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            cursorLock = false;
        else if (Input.GetMouseButtonDown(0))
            cursorLock = true;

        if (cursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
