using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    void Start()
    {
        StartCoroutine(WaitForRunner());
    }

    private System.Collections.IEnumerator WaitForRunner()
    {
        // Runner が見つかるまで待つ
        while (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
            yield return null;
        }

        Debug.Log("[RunnerHandler] Runner 発見！Callbacks 登録！");
        _runner.AddCallbacks(this);
    }


    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("ここまでは到達してるよ");
        if (!runner.IsServer)
            return;

        if (SceneManager.GetActiveScene().name != "GameScene")
            return;

        Debug.Log("[RunnerHandler] SceneLoadDone → 全プレイヤーをスポーン開始");

        var roomNetwork = FindObjectOfType<RoomNetwork>();
        if (roomNetwork == null)
        {
            Debug.LogError("RoomNetwork が見つかりません");
            return;
        }

        foreach (var player in runner.ActivePlayers)
        {
            int stableId = player.RawEncoded;
            bool isProtecter = (stableId == roomNetwork.ProtecterId);

            NetworkPrefabRef prefabToSpawn = isProtecter
                ? roomNetwork.ProtecterPrefab
                : roomNetwork.AttackerPrefab;

            Vector3 spawnPos = isProtecter
                ? new Vector3(550, 138, -838)
                : new Vector3(
                    UnityEngine.Random.Range(540f, 550f),
                    121f,
                    UnityEngine.Random.Range(-870f, -860f)
                );

            Debug.Log($"[SpawnCheck] Player={stableId}, Protecter={roomNetwork.ProtecterId}, IsProtecter={isProtecter}");

            runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player);
        }
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
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
}
