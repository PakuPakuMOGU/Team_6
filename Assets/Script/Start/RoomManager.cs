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
            // ホストだけRoomNetworkをSpawn.
            var netObj = _runner.Spawn(roomNetworkPrefab, Vector3.zero, Quaternion.identity);
        }

        // スタートボタンはホストのみ表示.
        startButton.SetActive(_runner.IsServer); 
        leaveButton.SetActive(true); 

        // プレイヤーの人数を表示.
        UpdatePlayerCount();
    }

    // Spawnした後にNetworkを取得.
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
            Debug.Log("RoomNetwork をクライアント側で取得しました");   // これが出なかったら絶望して.
        }
    }

    // プレイヤー人数表示.
    private void UpdatePlayerCount()
    {
        Debug.Log("UpdatePlayerCount called");
        if (_runner != null)
        {
            // 同じ部屋にいる人数を数える処理.
            int count = _runner.ActivePlayers.Count();
            // 現在の人数を表示.
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
            _netRoom.RpcSetFaction(faction);

        // 陣営選択画面を閉じる.
        SelectPanelView.WindowClose();
    }

    // 振り分けた陣営を表示.
    public void OnFactionAssigned(int protecterId)
    {
        var localId = _runner.LocalPlayer.PlayerId;
        Debug.Log($"[OnFactionAssigned] Local={localId}, Protecter={protecterId}");

        // ホストのprotectedIdが-1になっちゃうから直打ちで合わせた.こんなひどいプログラムを許すな.
        // IDで陣営を認識してそれぞれの陣営用の画面を表示.
        if (localId == protecterId || (localId == 1 && -1 == protecterId)) 
        {
            Debug.Log("→ この端末は Protecter");
            protecterView.WindowView();
        }
        else
        {
            Debug.Log("→ この端末は Attacker");  // 両方にAttackerが表示されるときはprotexterIdを疑う.-1かも.
            attackerView.WindowView();
        }
    }

    // プレイヤー参加時に人数反映.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // プレイヤーオブジェクトをspawn.
        var obj = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
        // プレイヤーの人数を更新.
        UpdatePlayerCount();
    }

    // ルームから退出.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // プレイヤーの人数を更新.
        UpdatePlayerCount();
    }

    // ゲーム開始（ホストのみ可）
    public void StartGame()
    {
        if (!_runner.IsServer)
        {
            Debug.Log("ホストのみ開始可能です");   // ホスト以外にボタンは表示されない.これが出力されたらバグ.
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