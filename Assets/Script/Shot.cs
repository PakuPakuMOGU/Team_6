using UnityEngine;
using System.Collections;

public class Shot : MonoBehaviour
{
    public UnityEngine.Camera mainCam;


    public AudioSource gunshotClip; // 銃声
    public AudioSource echoClip;    // 薬莢
    public AudioSource GunHit;//
    public float rayDistance = 5f;

    private AudioSource audioSource;
    private Animator animator;

    [SerializeField] private int damage = 10;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        animator = GetComponent<Animator>();

        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0f; // 2Dサウンド
    }

    void Update()
    {


        if (Input.GetMouseButtonDown(0))
        {

            // 画面中央からRayを飛ばす
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = mainCam.ScreenPointToRay(screenCenter);
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                HandleHit(hit.collider);
            }

            // 🔊 銃声を再生
            if (gunshotClip != null)
            {
                gunshotClip.Play();
            }

            Debug.Log("ばーん");

            // アニメーション再生
            animator.SetBool("Shot", true);
            StartCoroutine(ResetShootFlag());

            if (echoClip != null)
            {
                StartCoroutine(PlayDelayedSound(2f, echoClip));
            }

        }
    }


    void HandleHit(Collider col)
    {
        // CompareTagの方が安全だけど、switchするならtag文字列で分岐が手軽
        switch (col.tag)
        {
            case "Fence2":
            case "Fence1":
            case "Land":
            case "S_Robo":
            case "G_Robo":
            case "T_Robo":
            case "B_Tare":
            case "C_Tare":
                DoSomething(col.gameObject);
                break;

            default:
                // それ以外のタグ
                break;
        }
    }

    void DoSomething(GameObject target)
    {
        GunHit.Play();

        target.GetComponentInParent<Enemy_HP>()?.TakeDamage(damage);

        Debug.Log($"Enemy/trap 共通処理：{target.name}");
        // 共通処理を書く
    }






    IEnumerator PlayDelayedSound(float delay, AudioSource source)
    {
        yield return new WaitForSeconds(delay);
        source.Play();
    }

    IEnumerator ResetShootFlag()
    {
        yield return null; // 1フレーム後にfalse
        animator.SetBool("Shot", false);
    }
}

