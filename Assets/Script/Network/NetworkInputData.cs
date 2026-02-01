using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 direction;
    public float yaw;
    public bool jumpPressed;
}