using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class Room : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject leaveButton;
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private View SelectPanelView;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, GameObject> _playerItems = new();
    private Dictionary<PlayerRef, string> _playerNames = new();
    private Dictionary<NetAddress, string> _pendingNames = new();
    private Dictionary<PlayerRef, string> _playerFactions = new(); // 陣営保持用.

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.Log("NetworkRunnerが見つかりません。スタートシーンから遷移しているか確認してください。");
            SceneManager.LoadScene("StartScene");
            return;
        }

        startButton.SetActive(_runner.IsServer);    // スタートボタンはホストのみ表示.
        leaveButton.SetActive(true);                // 退出ボタンの表示.
    }

    // 陣営選択画面表示.
    public void OnSelectFactionPanel()
    {
        SelectPanelView.WindowView();
    }

    // 陣営選択処理.
    public void OnSelectFaction(string faction)
    {
        Debug.Log($"選択された陣営: {faction}");

        // RPC呼び出しはNetworkBehaviour経由
        var netRoom = _runner.GetComponent<RoomNetwork>();
        if (netRoom != null)
        {
            netRoom.RpcSetFaction(_runner.LocalPlayer, faction);
        }

        SelectPanelView.WindowClose();
    }

    // 接続要求時に名前受け取り.
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        if (token != null && token.Length > 0)
        {
            string name = System.Text.Encoding.UTF8.GetString(token);
            // Spawn時にStartPlayerへ渡す.
            PlayerInfo.PendingName = name;
        }
    }

    // プレイヤー参加時に名前反映.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        var obj = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);

        string name = $"Player {player.PlayerId}";
        if (obj.TryGetComponent<StartPlayer>(out var startPlayer))
        {
            name = startPlayer.PlayerName;
        }

        var item = Instantiate(playerItemPrefab, playerListParent);
        item.GetComponentInChildren<TextMeshProUGUI>().text = name;
        _playerItems[player] = item;
        _playerNames[player] = name;
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
        _playerNames.Remove(player);
        _playerFactions.Remove(player);
    }

    // ゲーム開始（ホストのみ可）
    public void StartGame()
    {
        int playerCount = _runner.ActivePlayers.Count();

        if (!_runner.IsServer)
        {
            Debug.Log("ホストのみ開始可能です");
            return;
        }

        if (playerCount < 2)
        {
            Debug.Log("プレイヤーが2人未満のため開始できません");
            return;
        }

        // 陣営選択開始
        var netRoom = _runner.GetComponent<RoomNetwork>();
        if (netRoom != null)
        {
            netRoom.RpcStartFactionSelection();
        }

        // 全員をゲームシーンへ移行
        SceneManager.LoadScene("GameScene");
    }

    // ルームから抜ける
    public void LeaveRoom()
    {
        foreach (var item in _playerItems.Values)
            Destroy(item);
        _playerItems.Clear();
        _playerNames.Clear();
        _pendingNames.Clear();
        _runner.Shutdown();
        SceneManager.LoadScene("StartScene");
    }

    // INetworkRunnerCallbacks空実装
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
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