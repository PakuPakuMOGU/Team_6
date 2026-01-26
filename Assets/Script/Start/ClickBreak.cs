using UnityEngine;
using UnityEngine.UI;

public class ClickBreak : MonoBehaviour
{
    public Image LogoImage;

    public float finishSpeed = 2f;   // フェードアウトの速さ.
    public float waitTime = 2f;      // 終了までの時間.

    private float timer = 0f;
    private enum State { FadeIn, Wait, FadeOut, Finish }
    private State state = State.FadeIn;

    void Start()
    {
        LogoImage.gameObject.SetActive(true);
    }

    void Update()
    {
        // タスク管理.
        switch (state)
        {
            case State.FadeOut:
                FadeOut();
                break;

            case State.Finish:
                Color c = LogoImage.color;
                c.a = 1f;
                LogoImage.color = c;
                break;
        }
    }

    public void Crick()
    {
        state = State.FadeOut;
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