using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private NetworkProjectConfig _newNetworkConfig;

    async void Start()
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            SceneManager = sceneManager
        });

        if (result.Ok)
            Debug.Log("部屋参加成功！");
        else
            Debug.LogError($"参加失敗: {result.ShutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Vector3 spawnPos = new Vector3(
            UnityEngine.Random.Range(540f, 550f),
            122f,
            UnityEngine.Random.Range(-860f, -870f)
        );

        // Host 自分プレイヤーに InputAuthority を渡す
        PlayerRef authority = player == runner.LocalPlayer ? runner.LocalPlayer : player;

        runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, authority);
    }

    // ────── INetworkRunnerCallbacks の空実装 ──────
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
}
