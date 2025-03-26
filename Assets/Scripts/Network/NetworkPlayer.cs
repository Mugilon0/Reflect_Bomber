using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; } // ���͌���������v���C���[�̓��[�J��



    // Start is called before the first frame update
    void Start()
    {

    }

    public override void Spawned() //�v���C���[���������ꂽ�ꍇ�ɌĂяo�����
    {
        if (Object.HasInputAuthority) // ����Ȃ��ƑS�ẴN���C�A���g�Ŏ��s�����1
        {
            Local = this; // ���[�J���v���C���[�̎Q��
            Debug.Log("Spawned local player");
        }
        else
        {
            Debug.Log("Spawned remote player"); //�����łȂ��Ȃ烊���[�g�v���C���[
        }
    }

    public void PlayerLeft(PlayerRef player) // �v���C���[�������Ƃ��̏���
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
