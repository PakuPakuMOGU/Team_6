using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject roomButtonPrefab;
    [SerializeField] private Transform roomListParent;
    [SerializeField] private TMP_InputField roomNameInput;

    private NetworkRunner _runner;

    async void Start()
    {
        await StartLobbyRunner();
    }

    // ロビー用RunnerをSharedモードで起動
    private async Task StartLobbyRunner()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        GameObject runnerObj = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "",
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError($"ロビーRunner起動失敗: {result.ShutdownReason}");
        }
    }

    // Runnerを再起動して指定モードで開始
    private async Task RestartRunner(GameMode mode, string sessionName)
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        // Runner専用GameObjectを作成
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
            Debug.LogError($"Runner起動失敗: {result.ShutdownReason}");
        }
    }

    // ルーム作成（ホスト）
    public async void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "DefaultRoom" : roomNameInput.text;
        await RestartRunner(GameMode.Host, roomName);
        SceneManager.LoadScene("RoomScene"); // ルーム画面へ
    }

    // ルーム参加（クライアント）
    public async void JoinRoom(string roomName)
    {
        await RestartRunner(GameMode.Client, roomName);
        SceneManager.LoadScene("RoomScene");
    }

    // セッション一覧更新
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        foreach (Transform child in roomListParent)
            Destroy(child.gameObject);

        foreach (var session in sessionList)
        {
            var button = Instantiate(roomButtonPrefab, roomListParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";

            button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                JoinRoom(session.Name);
            });
        }
    }

    // INetworkRunnerCallbacks 空実装
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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