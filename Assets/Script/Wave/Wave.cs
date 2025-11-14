using UnityEngine;

public class Wave : MonoBehaviour
{
    [Header("フレームレート設定")]
    [SerializeField] private int targetFPS = 60;

    [Header("ウェーブ時間（秒）")]
    [SerializeField] private int wave1Duration = 120;
    [SerializeField] private View ViewWave_1;
    [SerializeField] private int wave2Duration = 120;
    [SerializeField] private View ViewWave_2;
    [SerializeField] private int wave3Duration = 120;
    [SerializeField] private int intervalBetweenWaves = 60;
    [SerializeField] private View ViewBetweenWave_A;
    [SerializeField] private View ViewBetweenWave_B;

    [Header("ゲーム終了処理")]
    public Sphere sphere;

    private enum WaveState { Wave1, Interval1, Wave2, Interval2, Wave3, Finished }
    private WaveState currentState = WaveState.Wave1;

    private int frameCounter = 0;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;
    }

    void Start()
    {
        // フレーム換算.
        wave1Duration *= targetFPS;
        wave2Duration *= targetFPS;
        wave3Duration *= targetFPS;
        intervalBetweenWaves *= targetFPS;
    }

    void Update()
    {
        frameCounter++;

        switch (currentState)
        {
            case WaveState.Wave1:
                if (frameCounter >= wave1Duration)
                {
                    TransitionTo(WaveState.Interval1);
                    ViewWave_1.WindowView();
                }
                break;

            case WaveState.Interval1:
                if(frameCounter >= 300) ViewWave_1.WindowClose();
                if (frameCounter >= intervalBetweenWaves)
                {
                    TransitionTo(WaveState.Wave2);
                    // 準備フェーズ終了.チームごとに表示分け.
                }
                break;

            case WaveState.Wave2:
                if (frameCounter >= wave2Duration)
                {
                    TransitionTo(WaveState.Interval2);
                    ViewWave_2.WindowView();
                }
                break;

            case WaveState.Interval2:
                if (frameCounter >= 300) ViewWave_2.WindowClose();
                if (frameCounter >= intervalBetweenWaves)
                {
                    TransitionTo(WaveState.Wave3);
                    // 準備フェーズ終了.チームごとに表示分け.
                }
                break;

            case WaveState.Wave3:
                if (frameCounter >= wave3Duration)
                    EndGame();
                break;

            case WaveState.Finished:
                break;
        }
    }

    private void TransitionTo(WaveState nextState)
    {
        currentState = nextState;
        frameCounter = 0;
        Debug.Log($"状態遷移: {nextState}");
    }

    private void EndGame()
    {
        currentState = WaveState.Finished;
        sphere.GameFinish();
        Debug.Log("ゲーム終了！");
    }
}