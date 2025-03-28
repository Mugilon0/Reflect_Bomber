using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{

    public static NetworkPlayer Local { get; set; } // ���͌���������v���C���[�̓��[�J��

    public Transform playerModel; // ���C���[��ς���̂�transform�ɃA�N�Z�X����K�v������

    //public CinemachineVirtualCamera virtualcamera;





    // Camera mode 



    // Start is called before the first frame update
    void Start()
    {

    }

    public override void Spawned() //�v���C���[���������ꂽ�ꍇ�ɌĂяo�����
    {
        if (Object.HasInputAuthority) // ����Ȃ��ƑS�ẴN���C�A���g�Ŏ��s�����1
        {
            Local = this; // ���[�J���v���C���[�̎Q��

            // Sets the layer of the local players model
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel")); //�@���C���[��ݒ�@�ڂ̓����_�����O����Ȃ��Ȃ�

            // Disable main camera
            Camera.main.gameObject.SetActive(false); //���[�J���J�������g�p����Ƃ��̓��C���J�����͖����ɂ���

            Debug.Log("Spawned local player");
        }
        else
        {
            // Disable the camera if we are not the local player
            Camera localCamera = GetComponentInChildren<Camera>(); // ���[�J���J�����ɃA�N�Z�X���A�q�ŃJ�������擾
            localCamera.enabled = false; // 5.11 // �����[�g�v���C���[�ł͖����ɂ���

            // Only 1 audio listener is allowed in the scene so disable remote players audio listener 
            AudioListener audioListener = GetComponentInChildren<AudioListener>();  // ���ɂ��Ă���Ɠ��l
            audioListener.enabled = false;


            Debug.Log("Spawned remote player"); //�����łȂ��Ȃ烊���[�g�v���C���[
        }
    }

    public void PlayerLeft(PlayerRef player) // �v���C���[�������Ƃ��̏���
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
