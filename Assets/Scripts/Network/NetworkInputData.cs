using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput //　プレイヤーの入力をネットワークを通じて送信 input.Set()に使用
{
    // floatなどは最適ではない
    public Vector2 movementInput; // x z
    public float rotationInput;
    //public NetworkBool isJumpPressed; // 今回は使わない
}
