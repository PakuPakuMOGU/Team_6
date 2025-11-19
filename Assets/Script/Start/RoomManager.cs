using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// RoomSceneはStartSceneから移動しないと動作しません.
public class Room : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject leaveButton;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, GameObject> _playerItems = new Dictionary<PlayerRef, GameObject>();

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("NetworkRunnerが見つかりません。スタートシーンから遷移しているか確認してください。");
            return;
        }

        startButton.SetActive(_runner.IsServer);    // スタートボタンはホストのみ表示.
        leaveButton.SetActive(true);                // 退出ボタンの表示.
    }

    // ルームに参加.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // プレイヤーリストにプレイヤー名を表示する予定.
        var item = Instantiate(playerItemPrefab, playerListParent);
        item.GetComponentInChildren<TextMeshProUGUI>().text = $"Player {player.PlayerId}";
        _playerItems[player] = item;
    }

    // ルームから退出.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // プレイヤー名をプレイヤーリストから削除.
        if (_playerItems.TryGetValue(player, out var item))
        {
            Destroy(item);
            _playerItems.Remove(player);
        }
    }

    // ゲーム開始（ホストのみ可）
    public void StartGame()
    {
        if (_runner.IsServer)
        {
            _runner.LoadScene("GameScene"); // ルーム内全員をGameSceneへ移動.
        }
    }

    // ルームから抜ける
    public void LeaveRoom()
    {
        _runner.Shutdown();
        SceneManager.LoadScene("StartScene"); // ロビー画面に戻る.
    }

    // INetworkRunnerCallbacks空実装.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
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