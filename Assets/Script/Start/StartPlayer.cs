using Fusion;
using UnityEngine;

public class StartPlayer : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            RpcSetName(PlayerInfo.PlayerName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RpcSetName(string name)
    {
        PlayerName = name;
    }
}