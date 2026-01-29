using System.Collections;
using UnityEngine;

public class SetActiveOnly : MonoBehaviour
{
    public GameObject nanika;

    // 指定秒後に表示.
    public void OpenAfter(float seconds)
    {
        StartCoroutine(OpenDelay(seconds));
    }

    // 指定秒後に非表示.
    public void CloseAfter(float seconds)
    {
        StartCoroutine(CloseDelay(seconds));
    }

    IEnumerator OpenDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        nanika.SetActive(true);
    }

    IEnumerator CloseDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        nanika.SetActive(false);
    }

    // 即時表示・非表示.
    public void Open()
    {
        nanika.SetActive(true);
    }

    public void Close()
    {
        nanika.SetActive(false);
    }
}