using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject roomButtonPrefab;
    [SerializeField] private GameObject createRoomButtonPrefab;
    [SerializeField] private GameObject RoomNamePrefab;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Transform roomListParent;
    [SerializeField] private View maxPlayerView;
    [SerializeField] private View noRoomView;

    private List<SessionInfo> _cachedSessionList = new List<SessionInfo>();
    private NetworkRunner _runner;


    private void Start()
    {
        // ルーム名入力.
        if (roomNameInput?.placeholder != null)
        {
            var placeholderText = roomNameInput.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                placeholderText.text = "RoomName";
            }
        }
    }

    // ロビー用Runnerの起動.    
    private async Task StartLobbyRunner()
    {
        if (_runner != null)
            await _runner.Shutdown();

        GameObject runnerObj = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "", 
            SceneManager = sceneManager
        });

        if (!result.Ok)
            Debug.LogError($"ロビーRunner起動失敗: {result.ShutdownReason}");
    }

    // Runnerの再起動.
    private async Task<StartGameResult> RestartRunner(GameMode mode, string sessionName)
    {
        if (_runner == null)
            _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
            await _runner.Shutdown();

        GameObject runnerObj = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.Log($"Runner起動失敗: {result.ShutdownReason}");
        }

        return result;
    }

    // ルーム作成（ホスト）
    // ルーム名決定.
    public async void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "DefaultRoom" : roomNameInput.text;

        // セッション名の衝突チェック.
        bool nameExists = _cachedSessionList.Any(s => s.Name == roomName);
        if (nameExists)
        {
            Debug.LogWarning("同名のルームが既に存在します");
            return;
        }

        await RestartRunner(GameMode.Host, roomName);
        SceneManager.LoadScene("RoomScene");
    }

    // ルーム参加（クライアント）
    public async void JoinRoom(string roomName)
    {
        var targetSession = _cachedSessionList.FirstOrDefault(s => s.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));
        if (targetSession == null)
        {
            Debug.LogWarning($"指定されたルーム {roomName} が見つかりません");
            return;
        }

        if (targetSession.PlayerCount >= 5)
        {
            maxPlayerView?.WindowView();
            return;
        }

        await RestartRunner(GameMode.Client, roomName);
        SceneManager.LoadScene("RoomScene"); // Runnerを引き継ぐ.
    }

    // ロビーへ参加.
    public async void EnterLobby()
    {
        await StartLobbyRunner();   // セッション名は渡さない.
        noRoomView?.WindowView();   // 初期状態は NoRoom を表示.
    }

    // ルームに参加.
    public async void OnClickJoinByNameDirect()
    {
        string inputName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(inputName))
        {
            Debug.LogWarning("ルーム名が空です");
            return;
        }

        // 接続を試みる
        var result = await RestartRunner(GameMode.Client, inputName);

        if (result != null && result.Ok)
        {
            // 成功した場合のみシーン遷移
            SceneManager.LoadScene("RoomScene");
        }
        else
        {
            // 失敗時はウィンドウ表示に留める
            Debug.Log($"接続失敗: {result?.ShutdownReason}");
            noRoomView?.WindowView();
        }
    }

    // ゲーム終了用関数
    public async void QuitGame()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
            _runner = null;
        }

        // アプリケーション終了（エディタとビルドで挙動を分ける）
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // INetworkRunnerCallbacks 空実装
    public void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason failed) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> session) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
}