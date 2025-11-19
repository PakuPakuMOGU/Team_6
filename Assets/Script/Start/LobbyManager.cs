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
    [SerializeField] private GameObject createRoomButtonPrefab;
    [SerializeField] private GameObject RoomNamePrefab;
    [SerializeField] private Transform roomListParent;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private View maxPlayerView;

    private List<SessionInfo> _cachedSessionList = new List<SessionInfo>();
    private NetworkRunner _runner;

    async void Start()
    {
        if (roomNameInput?.placeholder != null)
        {
            var placeholderText = roomNameInput.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                placeholderText.text = "RoomName";
            }
        }
    }

    // ロビー用Runnerをで起動.    
    private async Task StartLobbyRunner(string sessionName)
    {
        // runnerが存在している場合はシャットダウン.
        if (_runner != null)
            await _runner.Shutdown();

        // Scene以降によるrunnnerの破棄を防止.
        GameObject runnerObj = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        // セッション開始.
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = sceneManager
        });

        if (!result.Ok)
            Debug.LogError($"ロビーRunner起動失敗: {result.ShutdownReason}");
    }

    // Runnerを再起動.
    private async Task RestartRunner(GameMode mode, string sessionName)
    {
        // runnerが存在している場合はシャットダウン.
        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        // Scene以降によるrunnnerの破棄を防止.
        GameObject runnerObj = new GameObject("NetworkRunner");
        DontDestroyOnLoad(runnerObj);

        _runner = runnerObj.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();

        // セッション開始.
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
    // ルームの名前決め.
    public void ShowRoomNameInputPanel()
    {
        RoomNamePrefab.SetActive(true);
    }
    // ルーム名決定.
    public async void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "DefaultRoom" : roomNameInput.text;

        await RestartRunner(GameMode.Host, roomName);

        SceneManager.LoadScene("RoomScene");
    }

    // ルーム参加（クライアント）
    public async void JoinRoom(string roomName)
    {
        SessionInfo targetSession = _cachedSessionList.Find(s => s.Name == roomName);

        // 定員オーバー.
        if (targetSession != null && targetSession.PlayerCount >= targetSession.MaxPlayers)
        {
            if (maxPlayerView != null)
                maxPlayerView.WindowView();
            return;
        }

        await RestartRunner(GameMode.Client, roomName);
        SceneManager.LoadScene("RoomScene");
    }

    // ロビーへ参加.
    public async void EnterLobby()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "Lobby" : roomNameInput.text;
        await StartLobbyRunner(roomName);
    }

    // セッション一覧更新.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _cachedSessionList = sessionList;

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