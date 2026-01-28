using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

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

        if (result.Ok)  Debug.Log("部屋参加成功！");
        else            Debug.LogError($"参加失敗: {result.ShutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        var roomNetwork = FindObjectOfType<RoomNetwork>();
        if (roomNetwork == null)
        {
            Debug.LogError("RoomNetwork が見つかりません");
            return;
        }

        int protecterId = roomNetwork.ProtecterId;

        // Protecterかどうか.
        bool isProtecter = false;
        Debug.Log("playerID = " + player.PlayerId);
        Debug.Log("protectID = " + protecterId);
        if (player.PlayerId  == protecterId || (player.PlayerId == 1 && -1 == protecterId))
            isProtecter = true;

        // 陣営ごとにスポーンプレハブを切り替え.
        NetworkPrefabRef prefabToSpawn = isProtecter
            ? roomNetwork.ProtecterPrefab
            : roomNetwork.AttackerPrefab;

        // スポーン位置も陣営ごとに変える
        Vector3 spawnPos = isProtecter
            ? new Vector3(550, 138, -838)
            : new Vector3(
                UnityEngine.Random.Range(540f, 550f),
                121f,
                UnityEngine.Random.Range(-870f, -860f)
            );

        runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        data.jumpPressed = Input.GetKey(KeyCode.Space);
        data.rotation = Input.GetAxis("Mouse X");

        input.Set(data);
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Fusion shutdown: {shutdownReason}");
    }

    // ────── INetworkRunnerCallbacks の空実装 ──────
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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
