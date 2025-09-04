using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput //�@�v���C���[�̓��͂��l�b�g���[�N��ʂ��đ��M input.Set()�Ɏg�p
{
    // float�Ȃǂ͍œK�ł͂Ȃ�
    public Vector2 movementInput; // x z
    public Vector3 aimForwardVector;
    //public NetworkBool isJumpPressed; // ����͎g��Ȃ�

    public NetworkBool isGrenadeFireButtonPressed;
    //public NetworkBool isLongGrenadeFireButtonPressed;

    public NetworkBool isShortThrow;
    public NetworkBool isLongThrow;
    public float longThrowCharge; // �ǂ̂��炢���߂���

    // ���߃A�j���[�V���������p
    public NetworkBool IsCharging;
}
