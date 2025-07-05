using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // �R���[�`�����g���ꍇ

public class GameStateManager : NetworkBehaviour
{
    public enum EGameState
    {
        Off,
        Ready,
        Loading,
        Intro,
        Game,
        Outro,
        PostGame
    }

    [Networked(OnChanged = nameof(OnStateChangedCallback))]
    public EGameState Current { get; set; }
    public EGameState Previous { get; private set; }

    [Networked] private TickTimer DelayTimer { get; set; }
    [Networked] private EGameState DelayedState { get; set; }

    [Networked] private TickTimer ReadyCountdownTimer { get; set; }
    [Networked(OnChanged = nameof(OnReadyCountdownChangedCallback))]
    public byte ReadyCountdownValue { get; set; } // Ready�V�[���ł�N�b�O�\���p

    [Networked] private TickTimer IntroTimer { get; set; } // Intro�V�[���ł̃J�E���g�_�E���p


    protected StateMachine<EGameState> StateMachine = new StateMachine<EGameState>();
    public static GameStateManager Instance { get; private set; }

    public ReadyUIHandler readyUIHandler;
    public ChatUIHandler chatUIHandler;

    public NetworkRunnerHandler networkRunnerHandler;



    public override void Spawned()
    {
        DontDestroyOnLoad(gameObject);

        base.Spawned();
        if (Instance == null) { Instance = this; }
        else if (Instance != this) { Runner.Despawn(Object); return; }

        if (readyUIHandler == null) readyUIHandler = FindObjectOfType<ReadyUIHandler>(true);
        if (networkRunnerHandler == null) networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>(true);

        if (chatUIHandler == null) chatUIHandler = FindObjectOfType<ChatUIHandler>(true);

        // --- Ready��� ---
        StateMachine[EGameState.Ready].onEnter = prev =>
        {
            Debug.Log($"Entering Ready from {prev}");
            ReadyCountdownTimer = TickTimer.None;
            ReadyCountdownValue = 0; // Networked Property�����Z�b�g -> OnChanged���Ă΂�UI�����Z�b�g

            if (readyUIHandler != null)
            {
                readyUIHandler.gameObject.SetActive(true);
                readyUIHandler.SetButtonText("Ready");
                readyUIHandler.SetCountdownText("");
                readyUIHandler.SetLocalPlayerReadyState(false); // ���[�J���v���C���[�̏�����Ԃ����Z�b�g
            }
            if (Runner.IsServer) Runner.SessionInfo.IsOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        };

        StateMachine[EGameState.Ready].onExit = next =>
        {
            Debug.Log($"Exiting Ready to {next}");
            if (Runner.IsServer)
            {
                if (next == EGameState.Loading || next == EGameState.Intro) Runner.SessionInfo.IsOpen = false;
                Debug.Log("Ready�X�e�[�g���o�܂�");
            }
        };

        StateMachine[EGameState.Ready].onUpdate = () =>
        {
            if (Runner.IsServer)
            {
                // 1. ���݂̑S�v���C���[�̏�����Ԃ𖈉�m�F
                bool currentAllPlayersReady = true;
                if (NetworkPlayer.ActivePlayers.Count == 0 && Runner.GameMode != GameMode.Single)
                {
                    currentAllPlayersReady = false;
                }
                else // �v���C���[�����邩�V���O�����[�h�̏ꍇ�̂ݏڍ׃`�F�b�N
                {
                    foreach (var playerEntry in NetworkPlayer.ActivePlayers) // ActivePlayers ���C�e���[�g
                    {
                        NetworkPlayer player = playerEntry.Value;
                        if (player == null || !player.Object.IsValid) continue;

                        var outfitHandler = player.GetComponent<CharacterOutfitHandler>();
                        if (outfitHandler == null || !outfitHandler.isDoneWithCharacterSelection)
                        {
                            currentAllPlayersReady = false;
                            break;
                        }
                    }
                }

                // 2. �J�E���g�_�E���^�C�}�[�����s�����ۂ��ŏ����𕪊�
                if (ReadyCountdownTimer.IsRunning)
                {
                    // 2a. �J�E���g�_�E�����s��
                    if (!currentAllPlayersReady) // �N���������������������H
                    {
                        Debug.Log("SERVER: A player became Not Ready during countdown. Stopping countdown.");
                        ReadyCountdownTimer = TickTimer.None; // �^�C�}�[���~
                        ReadyCountdownValue = 0; // UI�\�������Z�b�g
                    }
                    else if (ReadyCountdownTimer.Expired(Runner)) // �S��Ready�̂܂܃J�E���g�_�E���I��
                    {
                        ReadyCountdownTimer = TickTimer.None;
                        Server_SetState(EGameState.Loading);
                        Debug.Log("Loading�ɑJ�ڂ��܂�");
                    }
                    else // �J�E���g�_�E���p��
                    {
                        byte newCountdownVal = (byte)Mathf.CeilToInt(ReadyCountdownTimer.RemainingTime(Runner) ?? 0);
                        if (ReadyCountdownValue != newCountdownVal)
                        {
                            ReadyCountdownValue = newCountdownVal;
                        }
                    }
                }
                else // 2b. �J�E���g�_�E�������s����Ă��Ȃ��ꍇ
                {
                    if (currentAllPlayersReady && (NetworkPlayer.ActivePlayers.Count >= 2))   // || Runner.GameMode == GameMode.Single
                    {
                        Debug.Log("All players ready, server starting countdown.");
                        ReadyCountdownTimer = TickTimer.CreateFromSeconds(Runner, 10); // 10�b�J�E���g�_�E��
                        ReadyCountdownValue = 10; // �����ɔ��f�����邽��
                    }
                    // else: �܂��S�����������ł͂Ȃ��A�܂��̓v���C���[�����Ȃ��̂ŉ������Ȃ�
                }
            }
        };

        // --- Loading��� ---
        StateMachine[EGameState.Loading].onEnter = prev =>
        {
            Debug.Log($"Entering Loading from {prev}");
            DelayTimer = TickTimer.None; // �ėp�x���^�C�}�[�����Z�b�g

            if (readyUIHandler != null)
            {
                readyUIHandler.gameObject.SetActive(true);
                readyUIHandler.SetCountdownText("Loading Game...");
                readyUIHandler.buttonReadyText.gameObject.SetActive(false);
            }



            if (Runner.IsServer)
            {
                Runner.SetActiveScene("World1"); // (Runner.SetActiveScene���Ăяo���z��)
                Debug.Log("SetActiveScene(World1)���Ă�");
            }
        };
        StateMachine[EGameState.Loading].onUpdate = () =>
        {
            if (Runner.IsServer)
            {
                // TODO: �S�v���C���[�� "World1" �V�[�����[�h������҂��W�b�N
                // ���̊����ʒm���󂯂� Intro ��Ԃ֑J�ڂ���̂����z
                // if (HaveAllPlayersLoadedScene("World1")) Server_SetState(EGameState.Intro);

                // ���̒x�� (���ۂ̃��[�h�����`�F�b�N�𐄏�)
                // 1�񂾂��x���Z�b�g����悤�ɂ���
                if (!DelayTimer.IsRunning && Previous == EGameState.Ready) // Ready���痈�āA�܂��x���Z�b�g���ĂȂ����
                {
                    // DelayTimer��Loading�ҋ@�Ɏg�� (Server_DelaySetState�͔ėp�Ȃ̂ł����ł͒���TickTimer���Z�b�g)
                    DelayTimer = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3�b���[�h�����Ɖ���
                    Debug.Log("Loading: Server will transition to Intro after 3 seconds (simulated load time).");
                }
                if (DelayTimer.IsRunning && DelayTimer.Expired(Runner))
                {
                    DelayTimer = TickTimer.None;
                    Server_SetState(EGameState.Intro);
                }
            }
        };
        StateMachine[EGameState.Loading].onExit = next => { /* ... */ };

        // --- Intro��� ---
        StateMachine[EGameState.Intro].onEnter = prev =>
        {
            Debug.Log("Intro�ɓ������I");
            Debug.Log($"Entering Intro from {prev}");
            IntroTimer = TickTimer.None; // Intro�p�^�C�}�[���Z�b�g

            // ���C��: InterfaceManager�o�R�ŃA�j���[�V�������J�n����
            if (InterfaceManager.Instance != null)
            {
                Debug.Log("Countdown�A�j���[�V�������Đ�");
                InterfaceManager.Instance.StartCountdown();
            }
            else
            {
                Debug.LogError("InterfaceManager.Instance is null. Cannot start countdown animation.");
            }

            if (Runner.IsServer)
            {
                foreach (var playerEntry in NetworkPlayer.ActivePlayers) // added 6/27
                {
                    if (playerEntry.Value != null)
                    {
                        // �ړ����b�N
                        var moveHandler = playerEntry.Value.GetComponent<CharacterMovementHandler>();
                        if (moveHandler != null)
                        {
                            // Intro�X�e�[�g�̒���(4�b)�ɍ��킹�āA4�b�Ԃ̃��b�N�^�C�}�[���Z�b�g
                            moveHandler.MoveLockTimer = TickTimer.CreateFromSeconds(Runner, 4.0f);
                        }

                        // �U�����b�N
                        var weaponHandler = playerEntry.Value.GetComponent<WeaponHandler>();
                        if (weaponHandler != null)
                        {
                            // 4�b�Ԃ̍U�����b�N�^�C�}�[���Z�b�g
                            weaponHandler.FireLockTimer = TickTimer.CreateFromSeconds(Runner, 4.0f);
                        }
                    }
                }


                // �A�j���[�V�����̒����ɍ��킹�Ēx�����Ԃ𒲐�
                // �Ⴆ�΃A�j���[�V������4�b�Ȃ�A4�b���Game�X�e�[�g��
                Server_DelaySetState(EGameState.Game, 4.0f);
            }
        };

        //// --- Intro��� ---
        //StateMachine[EGameState.Intro].onEnter = prev =>
        //{
        //    Debug.Log($"Entering Intro from {prev}");
        //    IntroTimer = TickTimer.None; // Intro�p�^�C�}�[���Z�b�g
        //    if (readyUIHandler != null)
        //    {
        //        readyUIHandler.gameObject.SetActive(true);
        //        readyUIHandler.countDownText.gameObject.SetActive(true);
        //        readyUIHandler.SetCountdownText("3"); // �����\��
        //    }
        //    if (Runner.IsServer)
        //    {
        //        IntroTimer = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3�b�̃C���g��
        //    }
        //};
        StateMachine[EGameState.Intro].onUpdate = () =>
        {
            if (IntroTimer.IsRunning) // �T�[�o�[�N���C�A���g������UI�X�V�̂��������ɂł���
            {
                //int remainingCeil = Mathf.CeilToInt(IntroTimer.RemainingTime(Runner) ?? 0);
                //if (readyUIHandler != null)
                //{
                //    if (remainingCeil > 0)
                //    {
                //        readyUIHandler.SetCountdownText(remainingCeil.ToString());
                //    }
                //    else if (IntroTimer.Expired(Runner)) // ���傤�ǏI�������e�B�b�N
                //    {
                //        readyUIHandler.SetCountdownText("GO!");
                //    }
                //}

                if (Runner.IsServer && IntroTimer.Expired(Runner))
                {
                    IntroTimer = TickTimer.None;
                    Server_SetState(EGameState.Game);
                }
            }
        };
        StateMachine[EGameState.Intro].onExit = next =>
        {
            Debug.Log($"Exiting Intro to {next}");
            if (readyUIHandler != null)
            {
                readyUIHandler.gameObject.SetActive(false);
            }
        };

        // (Game, Outro, PostGame �͓��l�ɒ�`)
        StateMachine[EGameState.Game].onEnter = prev => 
        { 
            Debug.Log("Entering Game"); 
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;

            if (Runner.IsServer)
            {
                if (RoundManager.Instance != null)
                {
                    RoundManager.Instance.TickStarted = Runner.Simulation.Tick;
                    Debug.Log($"�^�C�}�[���J�n���܂����BStart Tick: {RoundManager.Instance.TickStarted}");
                }
                else
                {
                    Debug.LogError("RoundManager�̃C���X�^���X��������܂���I");
                }
            }
            if (InterfaceManager.Instance != null && InterfaceManager.Instance.inGameUIScreen != null)
            {
                UIScreen.Focus(InterfaceManager.Instance.inGameUIScreen);
                //// ���̎��_�ł͂܂��Q�[���͎n�܂��Ă��Ȃ��̂ŁA�^�C�}�[�\�������������Ă���
                //HUD.SetTimerText(120.0f); // �Ⴆ��2������n�܂�Ȃ�A�ő�l���Z�b�g���Ă���
            }


        };

        StateMachine[EGameState.Game].onUpdate = () =>
        {
            Debug.Log("[EGameState.Game].onUpdate�ɓ���܂����I");
            //// RoundManager���猻�݂̌o�ߎ��Ԃ��擾����UI�ɕ\���@�����Ƃł�� 6/25
            //if (inGameScoreUIHandler != null)
            //{
            //    inGameScoreUIHandler.UpdateTimer(RoundManager.Time);
            //}
            float remainingTime = RoundManager.MaxTime - RoundManager.Time;

            // 2. 0�b�����ɂȂ�Ȃ��悤�ɒl��ۏ؂���
            float displayTime = Mathf.Max(0f, remainingTime);

            // 3. �c�莞�Ԃ�HUD�ɓn���ĕ\�����X�V����
            HUD.SetTimerText(displayTime);
            //HUD.SetTimerText(RoundManager.Time); // �J�E���g�_�E�������ɂ���@// RoundManager����ŐV�̌o�ߎ��Ԃ��擾 
            
            // �T�[�o�[��ł̂ݔ�����s���@���Ԍo�ߌ�̃V����
            if (Runner.IsServer && RoundManager.Time >= RoundManager.MaxTime)
            {
                // ���Ԑ؂�ɂȂ������̏����������ɏ���
                Debug.Log("���E���h�I���ł��I�I");

                // ��F�Q�[�����I���X�e�[�g�Ɉڍs������
                // Server_SetState(EGameState.Outro);
            }
        };

            StateMachine[EGameState.Game].onExit = next => { Debug.Log("Exiting Game"); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; };
            StateMachine[EGameState.Outro].onEnter = prev => { Debug.Log("Entering Outro"); if (Runner.IsServer) Server_DelaySetState(EGameState.PostGame, 5f); };
            StateMachine[EGameState.Outro].onExit = next => { Debug.Log("Exiting Outro"); };
            StateMachine[EGameState.PostGame].onEnter = prev => { Debug.Log("Entering PostGame"); /* �Đ킩�ޏo����UI�\�� */ if (Runner.IsServer) Server_DelaySetState(EGameState.Ready, 10f); /* ����10�b��Ready�� */};
            StateMachine[EGameState.PostGame].onExit = next => { Debug.Log("Exiting PostGame"); };


            if (Runner.IsServer)
            {
                // ������Ԑݒ胍�W�b�N
                string currentSceneName = SceneManager.GetActiveScene().name;
                if (currentSceneName == "Ready")
                {
                    Server_SetState(EGameState.Ready);
                    Debug.Log("Ready�X�e�[�g�ɏ�Ԃ��Z�b�g");
                }
                else if (currentSceneName == "World1") Server_SetState(EGameState.Loading); //World1����n�߂���Loading�o�R
                                                                                            // else Server_SetState(EGameState.Off); // �܂��̓G���[
            }
        }
       

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            if (DelayedState != EGameState.Off && DelayTimer.Expired(Runner))
            {
                EGameState targetState = DelayedState;
                DelayTimer = TickTimer.None;
                DelayedState = EGameState.Off;
                Server_SetState(targetState);
            }
        }
        if (Runner.IsForward || Runner.IsResimulation)
        {
            StateMachine.Update(Current, Previous);
        }
    }

    public void Server_SetState(EGameState newState)
    {
        if (!Runner.IsServer) return;
        if (Current == newState) return;
        Debug.Log($"Server: Changing state from {Previous} -> {Current} to {newState}");
        Previous = Current;
        Current = newState;
    }

    public void Server_DelaySetState(EGameState newState, float delay)
    {
        if (!Runner.IsServer) return;
        DelayTimer = TickTimer.CreateFromSeconds(Runner, delay);
        DelayedState = newState;
        Debug.Log($"Server: State {newState} will be set after {delay} seconds.");
    }

    private static void OnStateChangedCallback(Changed<GameStateManager> changed)
    {
        // This is called on clients when 'Current' state changes.
        // UI updates can also be triggered here if not handled by onEnter/onExit or specific OnChanged for other properties.
        Debug.Log($"Client {changed.Behaviour.Runner.LocalPlayer}: State changed from {changed.Behaviour.Previous} to {changed.Behaviour.Current}");
        // ������ onEnter/onExit �����s�������ꍇ�͂�����StateMachine.Update���ĂԂ��Ƃ��\�����A
        // FixedUpdateNetwork �Ƃ̏d���Ăяo������s�����ɒ��ӁB�ʏ��FixedUpdateNetwork�ɔC����B
        // changed.Behaviour.StateMachine.Update(changed.Behaviour.Current, changed.Behaviour.Previous);
    }

    private static void OnReadyCountdownChangedCallback(Changed<GameStateManager> changed)
    {
        // changed.Behaviour �� GameStateManager �̃C���X�^���X���w���A
        // ���̎��_�� changed.Behaviour.ReadyCountdownValue �͊��ɐV�����l��ێ����Ă��܂��B
        byte newVal = changed.Behaviour.ReadyCountdownValue;

        // �����Â��l�Ɣ�r�������ꍇ�́A��� LoadOld() ���Ăт܂�
        // changed.LoadOld();
        // byte oldVal = changed.Behaviour.ReadyCountdownValue;
        // changed.LoadNew(); // �K�v�Ȃ�V�����l�ɖ߂��i�ʏ�͕s�v�A�f�t�H���g�ŐV�����l�ɂȂ��Ă���j

        if (changed.Behaviour.readyUIHandler != null && changed.Behaviour.Current == EGameState.Ready)
        {
            Debug.Log($"Client {changed.Behaviour.Runner.LocalPlayer}: ReadyCountdownValue changed to {newVal}");
            if (newVal > 0)
            {
                changed.Behaviour.readyUIHandler.UpdateCountdownDisplay(newVal); //
            }
            else
            {
                // �J�E���g��0�ɂȂ������ɂ��邩�A"Waiting..."�ȂǓ���̃e�L�X�g��
                changed.Behaviour.readyUIHandler.SetCountdownText(""); //
            }
        }
    }

    // Intro�p�̃R���[�`���i�T�[�o�[�Ŏ��s���AUI�X�V��Networked Property�o�R���]�܂����j
    // System.Collections.IEnumerator IntroCountdownCoroutine()
    // {
    //     byte countdown = 3;
    //     while (countdown > 0)
    //     {
    //         // IntroCountdownValue = countdown; // Networked Property �ŃN���C�A���g�ɓ���
    //         if (readyUIHandler != null) readyUIHandler.SetCountdownText(countdown.ToString());
    //         yield return new WaitForSeconds(1);
    //         countdown--;
    //     }
    //     // IntroCountdownValue = 0;
    //     if (readyUIHandler != null) readyUIHandler.SetCountdownText("GO!");
    //     yield return new WaitForSeconds(0.5f);
    //
    //     if (Runner.IsServer) Server_SetState(EGameState.Game);
    // }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RelayChatMessage(string playerName, string message)
    {
        // ����RPC���󂯎�����S�N���C�A���g���A������UI�Ƀ��b�Z�[�W��ǉ�����
        if (chatUIHandler != null)
        {
            chatUIHandler.AddNewMessage(playerName, message);
        }
    }
}