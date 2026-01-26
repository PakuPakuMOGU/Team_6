using UnityEngine;
using UnityEngine.UI;

public class Logo : MonoBehaviour
{
    public Image BackImage;
    public Image LogoImage;

    public float fadeSpeed = 1f;     // フェードインの速さ.
    public float finishSpeed = 2f;   // フェードアウトの速さ.
    public float waitTime = 1f;      // 完全に表示されてから消すまでの時間.

    private float timer = 0f;
    private enum State { FadeIn, Wait, FadeOut, Finish }
    private State state = State.FadeIn;

    void Start()
    {
        // 背景を表示.
        BackImage.gameObject.SetActive(true);
        // ロゴは最初透明.
        LogoImage.gameObject.SetActive(true);
        Color c = LogoImage.color;
        c.a = 0f;
        LogoImage.color = c;
    }

    void Update()
    {
        // タスク管理.
        switch (state)
        {
            case State.FadeIn:
                FadeIn();
                break;

            case State.Wait:
                timer += Time.deltaTime;
                if (timer >= waitTime)
                {
                    state = State.FadeOut;
                }
                break;

            case State.FadeOut:
                FadeOut();
                break;

            case State.Finish:
                // 完了後の処理（非表示など）
                BackImage.gameObject.SetActive(false);
                LogoImage.gameObject.SetActive(false);
                break;
        }
    }

    // フェードイン.
    void FadeIn()
    {
        Color c = LogoImage.color;
        c.a += fadeSpeed * Time.deltaTime;
        LogoImage.color = c;

        if (c.a >= 1f)
        {
            c.a = 1f;
            LogoImage.color = c;
            timer = 0f;
            state = State.Wait;
        }
    }

    // フェードアウト.
    void FadeOut()
    {
        Color c = LogoImage.color;
        c.a -= finishSpeed * Time.deltaTime;
        LogoImage.color = c;

        if (c.a <= 0f)
        {
            c.a = 0f;
            LogoImage.color = c;
            state = State.Finish;
        }
    }
}