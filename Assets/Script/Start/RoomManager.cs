using Fusion;
using Fusion.Sockets;
using Fusion.Photon.Realtime;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class Room : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject leaveButton;
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private View SelectPanelView;
    [SerializeField] private SceneRef gameScene;
    [SerializeField] private View protecterView;
    [SerializeField] private View attackerView;
    [SerializeField] private NetworkObject roomNetworkPrefab;
    [SerializeField] private TextMeshPro playerCountText;

    private RoomNetwork _netRoom;
    private NetworkRunner _runner;
    private Dictionary<PlayerRef, GameObject> _playerItems = new();
    private Dictionary<PlayerRef, string> _playerFactions = new(); // 陣営保持用.

    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.Log("NetworkRunner が見つかりません");
            return;
        }

        _runner.AddCallbacks(this);

        if (_runner.IsServer)
        {
            // ホストだけ RoomNetwork を Spawn
            var netObj = _runner.Spawn(roomNetworkPrefab, Vector3.zero, Quaternion.identity);
            // Spawned() の中で Room に SetRoomNetwork が飛んでくる
        }

        startButton.SetActive(_runner.IsServer);    // スタートボタンはホストのみ表示.
        leaveButton.SetActive(true);                // 退出ボタンの表示.

        UpdatePlayerCount();
    }

    public void SetRoomNetwork(RoomNetwork rn)
    {
        _netRoom = rn;
        Debug.Log("RoomNetwork を Room にセットしました");
    }

    // RoomNetworkをクライアント側から取得.
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        if (obj.TryGetComponent<RoomNetwork>(out var rn))
        {
            _netRoom = rn;
            Debug.Log("RoomNetwork をクライアント側で取得しました");
        }
    }

    // プレイヤー人数表示.
    private void UpdatePlayerCount()
    {
        Debug.Log("UpdatePlayerCount called");
        if (_runner != null)
        {
            int count = _runner.ActivePlayers.Count();
            Debug.Log("Player count: " + count);
            playerCountText.text = $"{count}";
        }
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

        if (_netRoom != null)
        {
            _netRoom.RpcSetFaction(faction);
        }

        SelectPanelView.WindowClose();
    }

    public void OnFactionAssigned(PlayerRef protecter)
    {
        // 陣営ごとにウィンドウを表示.
        if (_runner.LocalPlayer == protecter)   protecterView.WindowView();      
        else                                    attackerView.WindowView();
    }

    // プレイヤー参加時に人数反映.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        var obj = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        var item = Instantiate(playerItemPrefab, playerListParent);
        _playerItems[player] = item;

        UpdatePlayerCount();
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
        _playerFactions.Remove(player);

        UpdatePlayerCount();
    }

    // ゲーム開始（ホストのみ可）
    public void StartGame()
    {
        if (!_runner.IsServer)
        {
            Debug.Log("ホストのみ開始可能です");
            return;
        }
        if (_runner.ActivePlayers.Count() < 2)
        {
            Debug.Log("プレイヤーが2人未満のため開始できません");
            return;
        }

        if (_netRoom != null)
        {
            _netRoom.RpcStartFactionSelection();
        }
        else
        {
            Debug.Log("RoomNetwork が見つかりません");
        }
    }

    // ルームから抜ける.
    public void LeaveRoom()
    {
        foreach (var item in _playerItems.Values)
            Destroy(item);
        _playerItems.Clear();
        _runner.Shutdown();
        SceneManager.LoadScene("StartScene");
    }

    // INetworkRunnerCallbacks空実装
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
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
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
}