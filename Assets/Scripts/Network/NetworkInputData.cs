using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput //@NetworkInput‚ğ’ñ‹Ÿ‚Å‚«‚é‚æ‚¤‚É‚È‚é
{
    // float‚È‚Ç‚ÍÅ“K‚Å‚Í‚È‚¢
    public Vector2 movementInput; // x z
    public float rotationInput;
    public NetworkBool isJumpPressed; // ¡‰ñ‚Íg‚í‚È‚¢
}
