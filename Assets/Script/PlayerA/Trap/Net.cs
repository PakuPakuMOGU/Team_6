using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class NetSaku2Controller : NetworkBehaviour
{
    [Networked]
    public Quaternion NetRotation { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            transform.rotation = NetRotation;
        }
    }
}
