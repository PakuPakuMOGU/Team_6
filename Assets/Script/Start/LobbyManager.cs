using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Linq;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject roomButtonPrefab;
    [SerializeField] private GameObject createRoomButtonPrefab;
    [SerializeField] private GameObject RoomNamePrefab;
    [SerializeField] private GameObject PlayerNamePrefab;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Transform roomListParent;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private View maxPlayerView;
    [SerializeField] private View playerNameView;
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
        // プレイヤー名入力.
        if (playerNameInput?.placeholder != null)
        {
            var placeholderText = playerNameInput.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                placeholderText.text = "PlayerName";
            }
        }
    }

    // ロビー用Runnerの起動.    
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
        var token = System.Text.Encoding.UTF8.GetBytes(PlayerInfo.PlayerName);

        // セッション開始.
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            SceneManager = sceneManager,
            ConnectionToken = token
        });

        if (!result.Ok)
            Debug.LogError($"ロビーRunner起動失敗: {result.ShutdownReason}");
    }

    // Runnerの再起動.
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
        var token = System.Text.Encoding.UTF8.GetBytes(PlayerInfo.PlayerName);

        // セッション開始.
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = sceneManager,
            ConnectionToken = token
        });

        if (!result.Ok)
        {
            Debug.LogError($"Runner起動失敗: {result.ShutdownReason}");
        }
    }

    // プレイヤー名の決定.
    public void ConfirmPlayerName()
    {
        string name = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            playerNameView.WindowView();
            Debug.LogWarning("プレイヤー名が空です");
            return;
        }
    }
    // 名前決定ボタンを押したら.
    public void CloseConfirmPlayerName()
    {
        string name = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("プレイヤー名が空です");
            return;
        }

        PlayerInfo.PlayerName = name;
        playerNameView.WindowClose();
    }

    // ルーム作成（ホスト）
    // ルームの名前決め.
    public void ShowRoomNameInputPanel()
    {
        Debug.Log("ShowRoomNameInputPanel called");

        PlayerNamePrefab.SetActive(true);

        if (playerNameInput == null)
        {
            Debug.LogError("playerNameInput is null");
            return;
        }

        string trimmedName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            Debug.LogWarning("プレイヤー名が空です");
            return;
        }

        Debug.Log("aaa");
        PlayerInfo.PlayerName = trimmedName;
        RoomNamePrefab.SetActive(true);
    }
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
        if (playerNameInput == null)
        {
            Debug.LogWarning("playerNameInput is null");
        }
        else
        {
            Debug.Log($"playerNameInput.text = '{playerNameInput.text}'");
        }
        if (string.IsNullOrEmpty(playerNameInput.text.Trim()))
        {
            playerNameView.WindowView(); // 名前入力ウィンドウを表示
            return;
        }

        PlayerInfo.PlayerName = playerNameInput.text.Trim();

        string roomName = string.IsNullOrEmpty(roomNameInput.text) ? "Lobby" : roomNameInput.text;
        await StartLobbyRunner(roomName);
    }

    // セッション一覧更新.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _cachedSessionList = sessionList;

        // 既存のルームボタンをすべて削除.
        foreach (Transform child in roomListParent)
            Destroy(child.gameObject);

        // ルームが存在しない場合は画像を表示.
        if (sessionList.Count == 0)
        {
            if (noRoomView != null)
                noRoomView.WindowView();
            return;
        }
        
        // ルームがある場合はボタンを生成.
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