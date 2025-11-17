using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject roomButtonPrefab;
    [SerializeField] private Transform roomListParent;
    [SerializeField] private TMP_InputField roomNameInput;

    private NetworkRunner _runner;

    async void Start()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, // ロビー用
            SessionName = "",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

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

    // ルーム作成.
    // LobbyManager.cs
    public void CreateRoom()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) roomName = "DefaultRoom";

        PlayerPrefs.SetString("RoomName", roomName);
        PlayerPrefs.SetInt("IsHost", 1);

        SceneManager.LoadScene("GameScene"); 
    }

    public void JoinRoom(string roomName)
    {
        PlayerPrefs.SetString("RoomName", roomName);
        PlayerPrefs.SetInt("IsHost", 0);

        SceneManager.LoadScene("GameScene");
    }


    // ────── INetworkRunnerCallbacks 空実装 ──────
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