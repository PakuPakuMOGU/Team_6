using UnityEngine;
using System.Collections;

public class Shot : MonoBehaviour
{
    public UnityEngine.Camera mainCam;


    public AudioSource gunshotClip; // 銃声
    public AudioSource echoClip;    // 反響音など

    private AudioSource audioSource;
    private Animator animator;

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

            // 画面中央からRayを飛ばす
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = mainCam.ScreenPointToRay(screenCenter);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit: " + hit.collider.name);
            }
        }
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

