using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput //�@NetworkInput��񋟂ł���悤�ɂȂ�
{
    // float�Ȃǂ͍œK�ł͂Ȃ�
    public Vector2 movementInput; // x z
    public float rotationInput;
    public NetworkBool isJumpPressed; // ����͎g��Ȃ�
}
