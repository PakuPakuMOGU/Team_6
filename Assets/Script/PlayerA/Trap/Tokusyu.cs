
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class Tokusyu_MoveAttackBool : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 3.5f;
    [Tooltip("マウス回転のスケールに利用されます")]
    public float rotationSpeed = 720f;

    [Header("重力")]
    public float gravity = -9.81f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayers = ~0;
    public float groundedVerticalVelocity = -2f;

    [Header("入力（旧 Input Manager 想定）")]
    public string horizontalAxis = "Horizontal"; // A/D
    public string verticalAxis = "Vertical";   // W/S
    public int attackMouseButton = 0;            // Mouse0（左クリック）

    [Header("回転（マウス）")]
    public string mouseX = "Mouse X";
    [Tooltip("マウスXに掛ける倍率。rotationSpeed と乗算されます")]
    public float mouseSensitivity = 1.0f;

    [Header("攻撃（Bool制御）")]
    [Tooltip("攻撃中も移動できるようにするか")]
    public bool canMoveWhileAttacking = false;
    [Tooltip("攻撃中は回転も止めたい場合はチェックを外す")]
    public bool canRotateWhileAttacking = true;
    [Tooltip("アニメイベントが無いときの保険。>0 で自動的にOFFに戻す時間（秒）。0で無効")]
    public float attackAutoEndTime = 0f;

    // Components
    private CharacterController controller;
    private Animator animator;

    // Animator hashes
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");

    // State
    private Vector3 inputDir;  // (h, 0, v)
    private Vector3 velocity;  // yに重力を貯める
    private bool isGrounded;

    // 攻撃状態の内部キャッシュ（AnimatorのBoolと同期）
    private bool isAttacking;
    private float attackTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        ReadInput();
        HandleAttackInput();
        HandleMovement();
        UpdateAttackTimer();
    }

    private void ReadInput()
    {
        float h = 0f, v = 0f;
        if (!string.IsNullOrEmpty(horizontalAxis)) h = Input.GetAxisRaw(horizontalAxis);
        if (!string.IsNullOrEmpty(verticalAxis)) v = Input.GetAxisRaw(verticalAxis);

        inputDir = new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();
    }

    /// <summary>
    /// 左クリックで IsAttacking = true（Bool）
    /// 終了は Animation Event（OnAttackAnimationEnd）か、attackAutoEndTime で自動OFF
    /// </summary>
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(attackMouseButton) && animator)
        {
            // すでに攻撃中でも押したらONを維持（Boolなので再ONで変化は無いが問題なし）
            isAttacking = true;
            attackTimer = 0f; // タイマー再スタート
            animator.SetTrigger("Attack");
        }
    }

    /// <summary>
    /// マウスXでその場回転（yaw）、W/Sで前後、A/Dで左右（ストレーフ）。
    /// 攻撃中は canMoveWhileAttacking / canRotateWhileAttacking に従う。
    /// </summary>
    private void HandleMovement()
    {
        // --- 入力 ---
        float h = inputDir.x; // A/D
        float v = inputDir.z; // W/S

        // --- 回転：マウスXでyaw ---
        float yawInput = 0f;
        if (!string.IsNullOrEmpty(mouseX))
            yawInput = Input.GetAxisRaw(mouseX) * mouseSensitivity;

        bool allowRotate = !isAttacking || canRotateWhileAttacking;
        if (allowRotate && Mathf.Abs(yawInput) > 0.0001f)
        {
            float yaw = yawInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, yaw, 0f);
        }

        // --- 攻撃中の移動可否 ---
        bool allowMove = !isAttacking || canMoveWhileAttacking;
        if (!allowMove)
        {
            h = 0f;
            v = 0f;
        }

        // --- キャラ基準のストレーフ＋前後移動 ---
        Vector3 planarMove = transform.right * h + transform.forward * v;
        if (planarMove.sqrMagnitude > 1f) planarMove.Normalize(); // 斜め加速防止
        Vector3 horizontal = planarMove * moveSpeed;

        // --- 接地判定（CCのGrounded + Sphere補助） ---
        bool ccGrounded = controller.isGrounded;
        bool sphereGrounded = Physics.CheckSphere(
            transform.position + Vector3.down * (controller.height * 0.5f - controller.radius + 0.05f),
            groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);

        isGrounded = ccGrounded || sphereGrounded;

        // --- 重力 ---
        if (isGrounded && velocity.y < 0f)
            velocity.y = groundedVerticalVelocity;
        else
            velocity.y += gravity * Time.deltaTime;

        // --- 実移動 ---
        Vector3 moveWorld = new Vector3(horizontal.x, velocity.y, horizontal.z);
        controller.Move(moveWorld * Time.deltaTime);

        // --- アニメパラメータ更新 ---
        if (animator)
        {
            animator.SetFloat(HashSpeed, planarMove.magnitude);
            // 必要なら左右/前後個別：
            // animator.SetFloat("SpeedX", h);
            // animator.SetFloat("SpeedZ", v);
        }
    }

    /// <summary>
    /// アニメ終端の Animation Event から呼ぶ
    /// Attack のループを止めて Locomotion へ戻すために Bool をOFFにする
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        if (!animator) return;
        isAttacking = false;
        animator.SetBool(HashIsAttacking, false);
    }

    /// <summary>
    /// アニメイベントが無い場合の保険として、一定時間で自動OFF
    /// </summary>
    private void UpdateAttackTimer()
    {
        if (!isAttacking) return;
        if (attackAutoEndTime <= 0f) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackAutoEndTime)
        {
            OnAttackAnimationEnd();
        }
    }

#if UNITY_EDITOR
    // デバッグ：接地スフィアの可視化（任意）
    void OnDrawGizmosSelected()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Vector3 pos = transform.position + Vector3.down * (controller.height * 0.5f - controller.radius + 0.05f);
        Gizmos.DrawSphere(pos, groundCheckRadius);
    }
#endif
}
