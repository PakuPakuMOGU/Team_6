using UnityEngine;
using System.Collections;

public class Shot : MonoBehaviour
{
    private UnityEngine.Camera mainCam;

    public AudioClip gunshotClip; // 銃声
    public AudioClip echoClip;    // 2秒後に鳴らす音（例：反響音、リロード音など）

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0f; // 2Dサウンドにする

        if (gunshotClip != null)
        {
            Debug.Log("再生テスト");
            audioSource.PlayOneShot(gunshotClip);
        }
    }

    void Update()
    {
        if (mainCam == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            // 🔊 銃声を再生
            if (gunshotClip != null)
            {
                audioSource.PlayOneShot(gunshotClip);
            }

            // ⏱ 2秒後に別の音を再生
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
}