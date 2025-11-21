using UnityEngine;
using System.Collections;

public class Shot : MonoBehaviour
{
    public UnityEngine.Camera mainCam;


    public AudioClip gunshotClip; // 銃声
    public AudioClip echoClip;    // 反響音など

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
                audioSource.PlayOneShot(gunshotClip);
            }

            Debug.Log("ばーん");

            // アニメーション再生
            animator.SetBool("Shot", true);
            StartCoroutine(ResetShootFlag());

            // ⏱ 反響音を2秒後に再生
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

    IEnumerator PlayDelayedSound(float delay, AudioClip clip)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(clip);
    }

    IEnumerator ResetShootFlag()
    {
        yield return null; // 1フレーム後にfalse
        animator.SetBool("Shot", false);
    }
}

