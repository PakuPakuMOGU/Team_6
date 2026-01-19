
// EnemyMelee3D.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Tokusyu : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 720f;
    public bool canMoveWhileAttacking = false;

    [Header("攻撃")]
    public int damage = 10;
    public float attackDuration = 0.6f;        // アニメ長に合わせて
    public bool useCoroutineToEndAttack = false; // Animation Event未設定時の保険
    public float hitRange = 1.3f;              // 前方までの距離
    public float hitRadius = 0.55f;            // 当たりの半径
    public LayerMask hittableLayers;           // Playerが含まれるレイヤーを設定
    public string playerTag = "Player";        // 当てたい相手のタグ

    private CharacterController controller;
    private Animator animator;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");

    private bool isAttacking;
    private Vector3 inputDir;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        ReadInput();          // 左クリックのみで攻撃
        HandleMovement();     // Speed更新もここで
    }

    private void ReadInput()
    {
        // 移動入力（例：WASD）※不要なら削除OK
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDir = new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        // 左クリックで攻撃
        if (!isAttacking && Input.GetMouseButtonDown(0))
        {
            Debug.Log("左クリックされた");
            StartAttack();
        }
    }

    private void HandleMovement()
    {
        Vector3 move = (isAttacking && !canMoveWhileAttacking) ? Vector3.zero : inputDir;

        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (animator) animator.SetFloat(HashSpeed, move.magnitude); // 0:Idle, >0:Walk
    }

    private void StartAttack()
    {
        isAttacking = true;
        if (animator) animator.SetBool(HashIsAttacking, true);

        if (useCoroutineToEndAttack)
            StartCoroutine(Co_EndAttackAfter(attackDuration));
    }

    private System.Collections.IEnumerator Co_EndAttackAfter(float t)
    {
        yield return new WaitForSeconds(t);
        EndAttack();
    }

    // === Animation Event: 攻撃の当たりフレームで呼ぶ ===
    public void OnAttackHit()
    {
        // 前方にOverlapSphere
        Vector3 center = transform.position + transform.forward * hitRange;
        Collider[] hits = Physics.OverlapSphere(center, hitRadius, hittableLayers, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            if (!col || !col.gameObject.CompareTag(playerTag)) continue;

            // Player_HP を探してダメージ
            var hp = col.GetComponent<Player_HP>();
            if (!hp) hp = col.GetComponentInParent<Player_HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
                // 1人にだけ当てたい場合は break; でもOK
            }
        }
    }

    // === Animation Event: 攻撃アニメの終端で呼ぶ ===
    public void OnAttackAnimationEnd()
    {
        if (!useCoroutineToEndAttack) EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        if (animator) animator.SetBool(HashIsAttacking, false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.3f, 0.3f, 0.35f);
        Vector3 center = transform.position + transform.forward * hitRange;
        Gizmos.DrawSphere(center, hitRadius);
    }
#endif
}
