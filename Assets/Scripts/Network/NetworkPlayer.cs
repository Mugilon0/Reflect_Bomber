using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; } // ���͌���������v���C���[�̓��[�J��

    public static Dictionary<PlayerRef, NetworkPlayer> ActivePlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    public Transform playerModel; // ���C���[��ς���̂�transform�ɃA�N�Z�X����K�v������

    //public CinemachineVirtualCamera virtualcamera;

    [Networked(OnChanged = nameof(OnNickNameChanged))]  // �������Ǝ������Ȃ���ΗL���ɂ��܂��I�I�I�I�I�I�I�I�I
    public NetworkString<_16> nickName { get; set; } // �ő�T�C�Y16

    // Remote Client Token Hash
    [Networked] public int token { get; set; }

    public LocalCameraHandler localCameraHandler;

    // Camera mode 
    public bool is3rdPersonCamera { get; set; }


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
            if (Camera.main != null)
                Camera.main.gameObject.SetActive(false); //���[�J���J�������g�p����Ƃ��̓��C���J�����͖����ɂ���

            //Enabled 1 audio listner
            AudioListener audioListner = GetComponentInChildren<AudioListener>(true);
            audioListner.enabled = true;


            // Enabled the local camera
            localCameraHandler.localCamera.enabled = true;

            // Detach camera if enabled
            localCameraHandler.transform.parent = null;

            //localUI.SetActive(true);  // 4/26 ep5 45:37


            RPC_SetNickName(GameManager.instance.playerNickName); // rpc�𑗐M����

            // tps���_�Ŏ����̖��O�������Ȃ��悤�ɂ���
            playerNickNameTM.gameObject.SetActive(false);

            Debug.Log("Spawned local player");
        }
        else
        {

            // Enabled the local camera
            localCameraHandler.localCamera.enabled = false;

            //localUI.SetActive(false); //UI������

            // Only 1 audio listener is allowed in the scene so disable remote players audio listener 
            AudioListener audioListener = GetComponentInChildren<AudioListener>();  // ���ɂ��Ă���Ɠ��l
            audioListener.enabled = false;


            Debug.Log("Spawned remote player"); //�����łȂ��Ȃ烊���[�g�v���C���[
        }

        // �v���C���[�̉E�����`���邽�� 4/17
        ActivePlayers[Object.InputAuthority] = this;

        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}"; // ���������v���C���[�̖��O��ύX
    }

    public void PlayerLeft(PlayerRef player) // �v���C���[�������Ƃ��̏���
    {
        //if (Object.HasStateAuthority)
        //{
        //    if (Runner.TryGetPlayerObject(player, out NetworkObject playerLeftNetworkObject))
        //        Local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerLeftNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");
        //}  //  ���b�Z�[�WUI�͂Ȃ��̂ŏȗ�


        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

    static void OnNickNameChanged(Changed<NetworkPlayer> changed) // �Ăяo�����̂͂�����
    {
        Debug.Log($"{Time.time} OnHPChanged value {changed.Behaviour.nickName}");

        changed.Behaviour.OnNickNameChanged();
    }


    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");

        playerNickNameTM.text = nickName.ToString();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] //���͎ҁ��T�[�o�[ rpc����M����
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"RPC SetNickName { nickName}");
        this.nickName = nickName;
    }

    void OnDestroy()            // �J������ep�i�߂���������Ă��悢�@���ꂪ�Ȃ���hostmisration����
    {
        // Get rid of the local camera if we get destroyed as a new one will be spawned with the new Network player
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);
    }

}
