using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;
using UnityEngine.SocialPlatforms;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; } // ���͌���������v���C���[�̓��[�J��

    public static Dictionary<PlayerRef, NetworkPlayer> ActivePlayers = new Dictionary<PlayerRef, NetworkPlayer>();

    public Transform playerModel; // ���C���[��ς���̂�transform�ɃA�N�Z�X����K�v������

    //public CinemachineVirtualCamera virtualcamera;

    [Networked(OnChanged = nameof(OnNickNameChanged))]  // �������Ǝ������Ȃ���ΗL���ɂ��܂��I�I�I�I�I�I�I�I�I
    public NetworkString<_16> nickName { get; set; } // �ő�T�C�Y16

    [Networked(OnChanged = nameof(OnScoreChanged))]
    public int score { get; set; } = 0;

    private InGameScoreUIHandler scoreUIHandler;

    // Remote Client Token Hash
    [Networked] public int token { get; set; }

    public LocalCameraHandler localCameraHandler;

    // Camera mode 
    public bool is3rdPersonCamera { get; set; }


    // Start is called before the first frame update
    void Start()
    {

    }
    private void Awake()
    {
        // ���̃I�u�W�F�N�g���V�[���J�ڂŔj������Ȃ��悤�ɂ���
        DontDestroyOnLoad(this.gameObject);
    }

    public override void Spawned() //�v���C���[���������ꂽ�ꍇ�ɌĂяo�����
    {
        bool isReadyScene = SceneManager.GetActiveScene().name == "Ready";
        if (Object.HasInputAuthority) // ����Ȃ��ƑS�ẴN���C�A���g�Ŏ��s�����1
        {
            Local = this; // ���[�J���v���C���[�̎Q��

            if (isReadyScene)
            {
                Camera.main.transform.position = new Vector3(transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);

                //Disable local camera
                localCameraHandler.gameObject.SetActive(false);

                //Disable UI for local player
                //localUI.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
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
                localCameraHandler.gameObject.SetActive(true);

                // Detach camera if enabled
                localCameraHandler.transform.parent = null;

                //localUI.SetActive(true);  // 4/26 ep5 45:37

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }


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

        scoreUIHandler = FindObjectOfType<InGameScoreUIHandler>(true);
        //if (scoreUIHandler != null) // OnNickNameChanged()���ŌĂяo���̂ŕs�v�ɂȂ���
        //    scoreUIHandler.UpdateAllPlayerScores(); // �����X�R�A�̕\���������łł���
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

        if (scoreUIHandler != null)
            scoreUIHandler.UpdateAllPlayerScores();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] //���͎ҁ��T�[�o�[ rpc����M����
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"RPC SetNickName {nickName}");
        this.nickName = nickName;
    }

    void OnDestroy()
    {
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);

        // ActivePlayers ���玩�g���폜
        if (Object != null && Object.InputAuthority.IsValid && ActivePlayers.ContainsKey(Object.InputAuthority))
        {
            if (ActivePlayers[Object.InputAuthority] == this)
            {
                ActivePlayers.Remove(Object.InputAuthority);
            }
        }
    }

    // ������ �C���_ 3: OnEnable/OnDisable/OnSceneLoaded ���C�� ������
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded���Ăт܂�");
        if (Object == null || !Object.IsValid) return;

        Debug.Log($"NetworkPlayer {Object.Id}: OnSceneLoaded for scene '{scene.name}'.");

        // �V�����V�[���p�̏��������� (Spawned�̃��W�b�N���Q�l�ɍĐ���)
        bool isReadyScene = scene.name == "Ready";

        if (Object.HasInputAuthority) // ���[�J���v���C���[�̏���
        {
            if (isReadyScene)
            {
                // Ready�V�[���ɖ߂��Ă����ꍇ�Ȃǂ̐ݒ�
                localCameraHandler.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else // "World1" �Ȃǂ̃Q�[���V�[���ł̐ݒ�
            {
                Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));
                if (Camera.main != null) Camera.main.gameObject.SetActive(false);

                var audioListener = GetComponentInChildren<AudioListener>(true);
                if (audioListener != null) audioListener.enabled = true;

                localCameraHandler.localCamera.enabled = true;
                localCameraHandler.gameObject.SetActive(true);
                if (localCameraHandler.transform.parent != null) localCameraHandler.transform.parent = null;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else // �����[�g�v���C���[�̏���
        {
            localCameraHandler.localCamera.enabled = false;
            var audioListener = GetComponentInChildren<AudioListener>(true);
            if (audioListener != null) audioListener.enabled = false;
        }

        // �Q�[���V�[���ɓ������烊�X�|�[����v��
        if (!isReadyScene && Object.HasStateAuthority)
        {
            var ch = GetComponent<CharacterMovementHandler>();
            if (ch != null)
            {
                ch.RequestRespawn();
            }
            else
            {
                Debug.LogError($"CharacterMovementHandler not found on player {Object.Id}");
            }
        }
    }

    static void OnScoreChanged(Changed<NetworkPlayer> changed)
    {
        changed.Behaviour.OnScoreChanged();
    }

    private void OnScoreChanged()
    {
        //Debug.Log($"�X�R�A���ύX����܂���: {score}");

        //if (Object.HasInputAuthority)
        //{
        //    // ���[�J���݂̂�UI�X�V����悤�ɂ���
        //    GameManager.instance.inGameScoreUIHandler.UpdateScoreboard();
        //}
        //if (Object.HasInputAuthority)
        //{

        if (scoreUIHandler != null)
            scoreUIHandler.UpdateAllPlayerScores();
        //}

    }

    // NetworkPlayer.cs
    //public void InitializeForNewScene()
    //{
    //    Debug.Log($"NetworkPlayer {Object.Id}: InitializeForNewScene called in scene {SceneManager.GetActiveScene().name}.");

    //    if (Object == null || !Object.IsValid) return;

    //    if (SceneManager.GetActiveScene().name != "Ready")
    //    {
    //        if (Object.HasStateAuthority && Object.HasInputAuthority)
    //        {
    //            Spawned(); // �J������UI�Ȃǃ��[�J���ݒ�̍ēK�p
    //        }
    //        if (Object.HasStateAuthority)
    //        {
    //            GetComponent<CharacterMovementHandler>()?.RequestRespawn(); // ���X�|�[���v��
    //        }
    //    }
    //}





}