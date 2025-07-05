using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // コルーチンを使う場合

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
    public byte ReadyCountdownValue { get; set; } // ReadyシーンでのN秒前表示用

    [Networked] private TickTimer IntroTimer { get; set; } // Introシーンでのカウントダウン用


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

        // --- Ready状態 ---
        StateMachine[EGameState.Ready].onEnter = prev =>
        {
            Debug.Log($"Entering Ready from {prev}");
            ReadyCountdownTimer = TickTimer.None;
            ReadyCountdownValue = 0; // Networked Propertyをリセット -> OnChangedが呼ばれUIもリセット

            if (readyUIHandler != null)
            {
                readyUIHandler.gameObject.SetActive(true);
                readyUIHandler.SetButtonText("Ready");
                readyUIHandler.SetCountdownText("");
                readyUIHandler.SetLocalPlayerReadyState(false); // ローカルプレイヤーの準備状態をリセット
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
                Debug.Log("Readyステートを出ます");
            }
        };

        StateMachine[EGameState.Ready].onUpdate = () =>
        {
            if (Runner.IsServer)
            {
                // 1. 現在の全プレイヤーの準備状態を毎回確認
                bool currentAllPlayersReady = true;
                if (NetworkPlayer.ActivePlayers.Count == 0 && Runner.GameMode != GameMode.Single)
                {
                    currentAllPlayersReady = false;
                }
                else // プレイヤーがいるかシングルモードの場合のみ詳細チェック
                {
                    foreach (var playerEntry in NetworkPlayer.ActivePlayers) // ActivePlayers をイテレート
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

                // 2. カウントダウンタイマーが実行中か否かで処理を分岐
                if (ReadyCountdownTimer.IsRunning)
                {
                    // 2a. カウントダウン実行中
                    if (!currentAllPlayersReady) // 誰かが準備を解除したか？
                    {
                        Debug.Log("SERVER: A player became Not Ready during countdown. Stopping countdown.");
                        ReadyCountdownTimer = TickTimer.None; // タイマーを停止
                        ReadyCountdownValue = 0; // UI表示もリセット
                    }
                    else if (ReadyCountdownTimer.Expired(Runner)) // 全員Readyのままカウントダウン終了
                    {
                        ReadyCountdownTimer = TickTimer.None;
                        Server_SetState(EGameState.Loading);
                        Debug.Log("Loadingに遷移します");
                    }
                    else // カウントダウン継続
                    {
                        byte newCountdownVal = (byte)Mathf.CeilToInt(ReadyCountdownTimer.RemainingTime(Runner) ?? 0);
                        if (ReadyCountdownValue != newCountdownVal)
                        {
                            ReadyCountdownValue = newCountdownVal;
                        }
                    }
                }
                else // 2b. カウントダウンが実行されていない場合
                {
                    if (currentAllPlayersReady && (NetworkPlayer.ActivePlayers.Count >= 2))   // || Runner.GameMode == GameMode.Single
                    {
                        Debug.Log("All players ready, server starting countdown.");
                        ReadyCountdownTimer = TickTimer.CreateFromSeconds(Runner, 10); // 10秒カウントダウン
                        ReadyCountdownValue = 10; // 即座に反映させるため
                    }
                    // else: まだ全員準備完了ではない、またはプレイヤーがいないので何もしない
                }
            }
        };

        // --- Loading状態 ---
        StateMachine[EGameState.Loading].onEnter = prev =>
        {
            Debug.Log($"Entering Loading from {prev}");
            DelayTimer = TickTimer.None; // 汎用遅延タイマーをリセット

            if (readyUIHandler != null)
            {
                readyUIHandler.gameObject.SetActive(true);
                readyUIHandler.SetCountdownText("Loading Game...");
                readyUIHandler.buttonReadyText.gameObject.SetActive(false);
            }



            if (Runner.IsServer)
            {
                Runner.SetActiveScene("World1"); // (Runner.SetActiveSceneを呼び出す想定)
                Debug.Log("SetActiveScene(World1)を呼んだ");
            }
        };
        StateMachine[EGameState.Loading].onUpdate = () =>
        {
            if (Runner.IsServer)
            {
                // TODO: 全プレイヤーの "World1" シーンロード完了を待つロジック
                // この完了通知を受けて Intro 状態へ遷移するのが理想
                // if (HaveAllPlayersLoadedScene("World1")) Server_SetState(EGameState.Intro);

                // 仮の遅延 (実際のロード完了チェックを推奨)
                // 1回だけ遅延セットするようにする
                if (!DelayTimer.IsRunning && Previous == EGameState.Ready) // Readyから来て、まだ遅延セットしてなければ
                {
                    // DelayTimerをLoading待機に使う (Server_DelaySetStateは汎用なのでここでは直接TickTimerをセット)
                    DelayTimer = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3秒ロードしたと仮定
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

        // --- Intro状態 ---
        StateMachine[EGameState.Intro].onEnter = prev =>
        {
            Debug.Log("Introに入った！");
            Debug.Log($"Entering Intro from {prev}");
            IntroTimer = TickTimer.None; // Intro用タイマーリセット

            // ★修正: InterfaceManager経由でアニメーションを開始する
            if (InterfaceManager.Instance != null)
            {
                Debug.Log("Countdownアニメーションを再生");
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
                        // 移動ロック
                        var moveHandler = playerEntry.Value.GetComponent<CharacterMovementHandler>();
                        if (moveHandler != null)
                        {
                            // Introステートの長さ(4秒)に合わせて、4秒間のロックタイマーをセット
                            moveHandler.MoveLockTimer = TickTimer.CreateFromSeconds(Runner, 4.0f);
                        }

                        // 攻撃ロック
                        var weaponHandler = playerEntry.Value.GetComponent<WeaponHandler>();
                        if (weaponHandler != null)
                        {
                            // 4秒間の攻撃ロックタイマーをセット
                            weaponHandler.FireLockTimer = TickTimer.CreateFromSeconds(Runner, 4.0f);
                        }
                    }
                }


                // アニメーションの長さに合わせて遅延時間を調整
                // 例えばアニメーションが4秒なら、4秒後にGameステートへ
                Server_DelaySetState(EGameState.Game, 4.0f);
            }
        };

        //// --- Intro状態 ---
        //StateMachine[EGameState.Intro].onEnter = prev =>
        //{
        //    Debug.Log($"Entering Intro from {prev}");
        //    IntroTimer = TickTimer.None; // Intro用タイマーリセット
        //    if (readyUIHandler != null)
        //    {
        //        readyUIHandler.gameObject.SetActive(true);
        //        readyUIHandler.countDownText.gameObject.SetActive(true);
        //        readyUIHandler.SetCountdownText("3"); // 初期表示
        //    }
        //    if (Runner.IsServer)
        //    {
        //        IntroTimer = TickTimer.CreateFromSeconds(Runner, 3.0f); // 3秒のイントロ
        //    }
        //};
        StateMachine[EGameState.Intro].onUpdate = () =>
        {
            if (IntroTimer.IsRunning) // サーバークライアント両方でUI更新のきっかけにできる
            {
                //int remainingCeil = Mathf.CeilToInt(IntroTimer.RemainingTime(Runner) ?? 0);
                //if (readyUIHandler != null)
                //{
                //    if (remainingCeil > 0)
                //    {
                //        readyUIHandler.SetCountdownText(remainingCeil.ToString());
                //    }
                //    else if (IntroTimer.Expired(Runner)) // ちょうど終了したティック
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

        // (Game, Outro, PostGame は同様に定義)
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
                    Debug.Log($"タイマーを開始しました。Start Tick: {RoundManager.Instance.TickStarted}");
                }
                else
                {
                    Debug.LogError("RoundManagerのインスタンスが見つかりません！");
                }
            }
            if (InterfaceManager.Instance != null && InterfaceManager.Instance.inGameUIScreen != null)
            {
                UIScreen.Focus(InterfaceManager.Instance.inGameUIScreen);
                //// この時点ではまだゲームは始まっていないので、タイマー表示を初期化しておく
                //HUD.SetTimerText(120.0f); // 例えば2分から始まるなら、最大値をセットしておく
            }


        };

        StateMachine[EGameState.Game].onUpdate = () =>
        {
            Debug.Log("[EGameState.Game].onUpdateに入りました！");
            //// RoundManagerから現在の経過時間を取得してUIに表示　←あとでやる 6/25
            //if (inGameScoreUIHandler != null)
            //{
            //    inGameScoreUIHandler.UpdateTimer(RoundManager.Time);
            //}
            float remainingTime = RoundManager.MaxTime - RoundManager.Time;

            // 2. 0秒未満にならないように値を保証する
            float displayTime = Mathf.Max(0f, remainingTime);

            // 3. 残り時間をHUDに渡して表示を更新する
            HUD.SetTimerText(displayTime);
            //HUD.SetTimerText(RoundManager.Time); // カウントダウン方式にする　// RoundManagerから最新の経過時間を取得 
            
            // サーバー上でのみ判定を行う　時間経過後のショリ
            if (Runner.IsServer && RoundManager.Time >= RoundManager.MaxTime)
            {
                // 時間切れになった時の処理をここに書く
                Debug.Log("ラウンド終了です！！");

                // 例：ゲームを終了ステートに移行させる
                // Server_SetState(EGameState.Outro);
            }
        };

            StateMachine[EGameState.Game].onExit = next => { Debug.Log("Exiting Game"); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; };
            StateMachine[EGameState.Outro].onEnter = prev => { Debug.Log("Entering Outro"); if (Runner.IsServer) Server_DelaySetState(EGameState.PostGame, 5f); };
            StateMachine[EGameState.Outro].onExit = next => { Debug.Log("Exiting Outro"); };
            StateMachine[EGameState.PostGame].onEnter = prev => { Debug.Log("Entering PostGame"); /* 再戦か退出かのUI表示 */ if (Runner.IsServer) Server_DelaySetState(EGameState.Ready, 10f); /* 仮で10秒後Readyへ */};
            StateMachine[EGameState.PostGame].onExit = next => { Debug.Log("Exiting PostGame"); };


            if (Runner.IsServer)
            {
                // 初期状態設定ロジック
                string currentSceneName = SceneManager.GetActiveScene().name;
                if (currentSceneName == "Ready")
                {
                    Server_SetState(EGameState.Ready);
                    Debug.Log("Readyステートに状態をセット");
                }
                else if (currentSceneName == "World1") Server_SetState(EGameState.Loading); //World1から始めたらLoading経由
                                                                                            // else Server_SetState(EGameState.Off); // またはエラー
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
        // 即座に onEnter/onExit を実行したい場合はここでStateMachine.Updateを呼ぶことも可能だが、
        // FixedUpdateNetwork との重複呼び出しや実行順序に注意。通常はFixedUpdateNetworkに任せる。
        // changed.Behaviour.StateMachine.Update(changed.Behaviour.Current, changed.Behaviour.Previous);
    }

    private static void OnReadyCountdownChangedCallback(Changed<GameStateManager> changed)
    {
        // changed.Behaviour は GameStateManager のインスタンスを指し、
        // この時点で changed.Behaviour.ReadyCountdownValue は既に新しい値を保持しています。
        byte newVal = changed.Behaviour.ReadyCountdownValue;

        // もし古い値と比較したい場合は、先に LoadOld() を呼びます
        // changed.LoadOld();
        // byte oldVal = changed.Behaviour.ReadyCountdownValue;
        // changed.LoadNew(); // 必要なら新しい値に戻す（通常は不要、デフォルトで新しい値になっている）

        if (changed.Behaviour.readyUIHandler != null && changed.Behaviour.Current == EGameState.Ready)
        {
            Debug.Log($"Client {changed.Behaviour.Runner.LocalPlayer}: ReadyCountdownValue changed to {newVal}");
            if (newVal > 0)
            {
                changed.Behaviour.readyUIHandler.UpdateCountdownDisplay(newVal); //
            }
            else
            {
                // カウントが0になったら空にするか、"Waiting..."など特定のテキストに
                changed.Behaviour.readyUIHandler.SetCountdownText(""); //
            }
        }
    }

    // Intro用のコルーチン（サーバーで実行し、UI更新はNetworked Property経由が望ましい）
    // System.Collections.IEnumerator IntroCountdownCoroutine()
    // {
    //     byte countdown = 3;
    //     while (countdown > 0)
    //     {
    //         // IntroCountdownValue = countdown; // Networked Property でクライアントに同期
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
        // このRPCを受け取った全クライアントが、自分のUIにメッセージを追加する
        if (chatUIHandler != null)
        {
            chatUIHandler.AddNewMessage(playerName, message);
        }
    }
}