using Fusion;
using UnityEngine;

public class StartPlayer : NetworkBehaviour
{
    [Networked]
    public string PlayerName { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(PlayerInfo.PlayerName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        PlayerName = name;
    }
}