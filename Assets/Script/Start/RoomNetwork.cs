using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class RoomNetwork : NetworkBehaviour
{ 
    // 陣営保持用.
    [Networked] public int ProtecterId { get; set; }
    private Dictionary<PlayerRef, string> _playerFactions = new();

    // 陣営ごとのキャラクタープレハブ.
    public NetworkPrefabRef ProtecterPrefab;
    public NetworkPrefabRef AttackerPrefab;

    void Start()
    {
        if (Runner.IsServer)
        {
            _playerFactions[Runner.LocalPlayer] = "Protecter"; // またはデフォルト
        }
    }

    // 陣営選択をサーバーに伝えるRPC.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcSetFaction(string faction, RpcInfo info = default)
    {
        var player = info.Source;

        if (player == PlayerRef.None)
        {
            Debug.LogWarning("PlayerRef.None を無視");
            return;
        }

        _playerFactions[player] = faction;
    }

    private int GetStableID(PlayerRef player)
    {
        return player.RawEncoded;
    }
    public void SetFactionServer(PlayerRef player, string faction)
    {
        _playerFactions[player] = faction;
        Debug.Log($"[Server] {player.RawEncoded} が {faction} を希望しました");
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcStartFactionSelection()
    {
        // ホストのみ開始可能.
        if (!Runner.IsServer)   return;

        // プレイヤー数チェック.
        int playerCount = Runner.ActivePlayers.Count();
        if (playerCount < 2)
        {
            Debug.Log("プレイヤーが2人未満のため陣営選択を開始できません");
            return;
        }

        Debug.Log("陣営選択開始！");
        SelectProtecter();
    }

    // 防衛者の決定.
    private void SelectProtecter()
    {
        var protecterCandidates = new List<PlayerRef>();
        foreach (var kvp in _playerFactions)
        {
            if (kvp.Value == "Protecter")
                protecterCandidates.Add(kvp.Key);
        }

        PlayerRef chosenProtecter;

        // 防衛者の希望者無し.
        if (protecterCandidates.Count == 0)
        {
            var allPlayers = Runner.ActivePlayers.ToList();
            chosenProtecter = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
            Debug.Log($"Protecter希望者なし、ランダムで {chosenProtecter.PlayerId} を選出");
        }
        // 防衛者の希望者が一人.
        else if (protecterCandidates.Count == 1)
        {
            chosenProtecter = protecterCandidates[0];
            Debug.Log($"Protecter希望者1人、{chosenProtecter.PlayerId} を選出");
        }
        // 防衛者の希望者が二人以上.
        else
        {
            chosenProtecter = protecterCandidates[UnityEngine.Random.Range(0, protecterCandidates.Count)];
            Debug.Log($"Protecter希望者複数、ランダムで {chosenProtecter.PlayerId} を選出");
        }

        RpcNotifyProtecter(chosenProtecter.RawEncoded);
    }

    // ゲームシーン遷移.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcNotifyProtecter(int protecterId)
    {
        foreach (var kvp in _playerFactions)
        {
            Debug.Log($"Faction登録: Player={kvp.Key.RawEncoded}, Faction={kvp.Value}");
        }
        ProtecterId = protecterId;

        Debug.Log($"Protecterは {protecterId} に決定しました");

        var room = FindObjectOfType<Room>();
        if (room != null)
            room.OnFactionAssigned(protecterId);

        if (Runner.IsServer)
            StartCoroutine(DelayedSceneLoad(2f));
    }

    private IEnumerator DelayedSceneLoad(float second)
    {
        yield return new WaitForSeconds(second); // 2秒待つ
        Runner.LoadScene("GameScene");
    }

    public override void Spawned()
    {
        Debug.Log("RoomNetwork Spawned");

        DontDestroyOnLoad(gameObject);

        var room = FindObjectOfType<Room>();
        if (room != null)
        {
            room.SetRoomNetwork(this);
        }
    }
}