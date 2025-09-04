using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput //　プレイヤーの入力をネットワークを通じて送信 input.Set()に使用
{
    // floatなどは最適ではない
    public Vector2 movementInput; // x z
    public Vector3 aimForwardVector;
    //public NetworkBool isJumpPressed; // 今回は使わない

    public NetworkBool isGrenadeFireButtonPressed;
    //public NetworkBool isLongGrenadeFireButtonPressed;

    public NetworkBool isShortThrow;
    public NetworkBool isLongThrow;
    public float longThrowCharge; // どのくらい溜めたか

    // 溜めアニメーション同期用
    public NetworkBool IsCharging;
}
