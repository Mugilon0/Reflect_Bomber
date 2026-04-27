using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement;
using Cinemachine;
using System;
using AAMAP;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    [Networked]
    public PlayerRef Ref { get; private set; }
    [Networked]
    public byte Index { get; private set; }
    public bool IsSpectator => GetComponent<PlayerState>()?.IsSpectator ?? false;
    //---------------------------




    public TextMeshProUGUI playerNickNameTM;
    public static NetworkPlayer Local { get; set; } 

    public Transform playerModel; 

    //public CinemachineVirtualCamera virtualcamera;

    [Networked(OnChanged = nameof(OnNickNameChanged))] 
    public NetworkString<_16> nickName { get; set; } 

    [Networked]
    public NetworkBool IsLoaded { get; set; } //

    // Remote Client Token Hash
    [Networked] public int token { get; set; }

    public LocalCameraHandler localCameraHandler;

    // Camera mode 
    //public bool is3rdPersonCamera { get; set; }


    // Start is called before the first frame update
    //void Start()
    //{

    //}
    private CharacterOutfitHandler _outfitHandler;
    private CharacterMovementHandler _movementHandler;
    private WeaponManager _weaponManager;
    private PlayerAnimationHandler _animationHandler;
    private HPHandler _hpHandler;




    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }


    public CharacterOutfitHandler OutfitHandler
    {
        get
        {
            if (_outfitHandler == null)
                _outfitHandler = GetComponent<CharacterOutfitHandler>();
            return _outfitHandler;
        }
    }

    public CharacterMovementHandler MovementHandler
    {
        get
        {
            if (_movementHandler == null)
                _movementHandler = GetComponent<CharacterMovementHandler>();
            return _movementHandler;
        }
    }

    public WeaponManager WeaponManager
    {
        get
        {
            if (_weaponManager == null)
                _weaponManager = GetComponent<WeaponManager>();
            return _weaponManager;
        }
    }

    public PlayerAnimationHandler AnimationHandler
    {
        get
        {
            if (_animationHandler == null)
                _animationHandler = GetComponent<PlayerAnimationHandler>();
            return _animationHandler;
        }
    }

    public HPHandler HPHandler
    {
        get
        {
            if (_hpHandler == null)
                _hpHandler = GetComponent<HPHandler>();
            return _hpHandler;
        }
    }



    public bool IsReady => GetComponent<PlayerState>()?.IsReady ?? false;
    public bool IsDead => HPHandler?.isDead ?? false;


    public void Lock()
    {
        if (MovementHandler != null) MovementHandler.IsLocked = true;
        if (WeaponManager != null) WeaponManager.IsLocked = true;
        if (AnimationHandler != null) AnimationHandler.IsLocked = true;
    }

    public void Unlock()
    {
        if (MovementHandler != null) MovementHandler.IsLocked = false;
        if (WeaponManager != null) WeaponManager.IsLocked = false;
        if (AnimationHandler != null) AnimationHandler.IsLocked = false;
    }


    public void LockAllActions(float duration)
    {
        if (MovementHandler != null)
            MovementHandler.MoveLockTimer = TickTimer.CreateFromSeconds(Runner, duration);
        if (WeaponManager != null)
            WeaponManager.LockWeapons(duration);
        if (AnimationHandler != null)
            AnimationHandler.AnimationLockTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    public void UnlockAllActions()
    {
        if (MovementHandler != null) MovementHandler.MoveLockTimer = TickTimer.None;
        if (WeaponManager != null) WeaponManager.LockWeapons(0f);
        if (AnimationHandler != null) AnimationHandler.AnimationLockTimer = TickTimer.None;
    }

    public override void Spawned() 
    {
        bool isReadyScene = SceneManager.GetActiveScene().name == "Ready";
        if (Object.HasInputAuthority)
        {
            Local = this;

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
                //Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));

                // Disable main camera
                if (Camera.main != null)
                    Camera.main.gameObject.SetActive(false);

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


            RPC_SetNickName(SaveDataManager.Current.NickName);


            playerNickNameTM.gameObject.SetActive(false);

            Debug.Log("Spawned local player");
        }
        else
        {

            // Enabled the local camera
            localCameraHandler.localCamera.enabled = false;

            //localUI.SetActive(false);

            // Only 1 audio listener is allowed in the scene so disable remote players audio listener 
            AudioListener audioListener = GetComponentInChildren<AudioListener>();  
            audioListener.enabled = false;


            Debug.Log("Spawned remote player"); 
        }



        // 4/17
        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}";

        //if (scoreUIHandler != null) // OnNickNameChanged()
        //    scoreUIHandler.UpdateAllPlayerScores(); 

        //if (Object.HasInputAuthority)
        //{

        //    if (InterfaceManager.Instance != null && InterfaceManager.Instance.readyMessageButtonSend != null)
        //    {
        //        InterfaceManager.Instance.readyMessageButtonSend.onPressed += OnSend;
        //    }
        //}



    }

    public void PlayerLeft(PlayerRef player)
    {
        //if (Object.HasStateAuthority)
        //{
        //    if (Runner.TryGetPlayerObject(player, out NetworkObject playerLeftNetworkObject))
        //        Local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerLeftNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");


        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

    static void OnNickNameChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time} OnHPChanged value {changed.Behaviour.nickName}");

        changed.Behaviour.OnNickNameChanged();
    }


    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");
        playerNickNameTM.text = nickName.ToString();
        // 削除: scoreUIHandler.RefreshAllScores();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] 
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"RPC SetNickName {nickName}");
        this.nickName = nickName;
    }


    //[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void OnDestroy()
    {
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);

        //if (Object != null && Object.HasInputAuthority)
        //{
        //    if (InterfaceManager.Instance != null && InterfaceManager.Instance.readyMessageButtonSend != null)
        //    {
        //        InterfaceManager.Instance.readyMessageButtonSend.onPressed -= OnSend;
        //    }
        //}
    }

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
        Debug.Log("OnSceneLoaded");
        if (Object == null || !Object.IsValid) return;

        Debug.Log($"NetworkPlayer {Object.Id}: OnSceneLoaded for scene '{scene.name}'.");


        bool isReadyScene = scene.name == "Ready";
        bool isWorld1Scene = scene.name == "World1";

        if (Object.HasInputAuthority) 
        {
            if (isReadyScene)
            {
                localCameraHandler.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (isWorld1Scene) 
            {
                //Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));
                if (Camera.main != null) Camera.main.gameObject.SetActive(false);

                var audioListener = GetComponentInChildren<AudioListener>(true);
                if (audioListener != null) audioListener.enabled = true;

                localCameraHandler.localCamera.enabled = true;
                localCameraHandler.gameObject.SetActive(true);
                if (localCameraHandler.transform.parent != null) localCameraHandler.transform.parent = null;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;



                // minimap関連

                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager == null)
                {
                    return;
                }

                MinimapManager minimapManager = uiManager.GetComponentInChildren<MinimapManager>(true);
                if (minimapManager == null)
                {
                    return;
                }

                MapIcon mapIcon = GetComponentInChildren<MapIcon>();
                if (mapIcon == null)
                {
                    return;
                }

                GameObject minimapCameraObject = GameObject.Find("Minimap Camera");
                if (minimapCameraObject == null)
                {
                    return;
                }
                else
                {
                    Debug.Log("Minimap cameraを見つけた");
                }

   

                mapIcon.SetMinimapCamera(minimapCameraObject);

      
                minimapManager.SetTargetObject(this.gameObject);

                minimapManager.SetCamera(minimapCameraObject);





            }
        }
        else
        {
            localCameraHandler.localCamera.enabled = false;
            var audioListener = GetComponentInChildren<AudioListener>(true);
            if (audioListener != null) audioListener.enabled = false;
        }

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

    // NetworkPlayer.cs
    //public void InitializeForNewScene()
    //{
    //    Debug.Log($"NetworkPlayer {Object.Id}: InitializeForNewScene called in scene {SceneManager.GetActiveScene().name}.");

    //    if (Object == null || !Object.IsValid) return;

    //    if (SceneManager.GetActiveScene().name != "Ready")
    //    {
    //        if (Object.HasStateAuthority && Object.HasInputAuthority)
    //        {
    //            Spawned();
    //        }
    //        if (Object.HasStateAuthority)
    //        {
    //            GetComponent<CharacterMovementHandler>()?.RequestRespawn();
    //        }
    //    }
    //}

    public void Server_Init(PlayerRef pRef, byte index)
    {
        Ref = pRef;
        Index = index;
    }


}