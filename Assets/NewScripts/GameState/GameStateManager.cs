using Fusion;
using System;
using System.Collections.Generic;
using System.Xml;
using Unity.Loading;
using UnityEngine;
using static Fusion.Sockets.NetConnectionMap;

public class GameStateManager : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnStateChangedCallback))]
    public EGameStateNet Current { get; private set; }
    public EGameStateNet Previous { get; private set; }

    public event Action<EGameStateNet> OnStateChanged;

    //public event Action<float> OnCountdownUpdated;
    //public event Action OnCountdownReset;
    //public void NotifyCountdownUpdated(float remaining) => OnCountdownUpdated?.Invoke(remaining);
    //public void NotifyCountdownReset() => OnCountdownReset?.Invoke();


    private StateMachine _stateMachine;
    private Dictionary<EGameStateNet, IGameState> _states;
    private IGameFlow _flowStrategy;

    [SerializeField] private int defaultMaxRounds = 4;
    [SerializeField] private int defaultRoundDuration = 120; //120

    [Networked] public TickTimer ReadyCountdownTimer { get; set; }
    [Networked] public TickTimer GameRoundTimer { get; set; }



    //[SerializeField] public ReadyUIHandler readyUIHandler;
    //[SerializeField] public ChatUIHandler chatUIHandler;
    [SerializeField] public NetworkRunnerHandler networkRunnerHandler;

    public static GameStateManager Instance { get; private set; }

    public override void Spawned()
    {

        if (Instance == null) { Instance = this; }
        else if (Instance != this) { Runner.Despawn(Object); return; }
        DontDestroyOnLoad(gameObject);

        //if (readyUIHandler == null)
        //    readyUIHandler = FindObjectOfType<ReadyUIHandler>(true);
        //if (chatUIHandler == null)
        //    chatUIHandler = FindObjectOfType<ChatUIHandler>(true);

        if (networkRunnerHandler == null)
            networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>(true);

        _flowStrategy = new OnlineGameFlow(defaultMaxRounds);


        _states = new Dictionary<EGameStateNet, IGameState>
        {
            //{ EGameStateNet.Lobby,    new LobbyState(Runner, networkRunnerHandler) },
            { EGameStateNet.Ready,    new ReadyState(Runner) },
            { EGameStateNet.Loading,  new LoadingState(Runner) },
            { EGameStateNet.Intro,    new IntroState(Runner) },
            { EGameStateNet.Game,     new GameState(Runner, defaultRoundDuration)},
            { EGameStateNet.Outro,    new OutroState(Runner) },
            { EGameStateNet.PostGame, new PostGameState(Runner) },
        };

        _stateMachine = new StateMachine(_flowStrategy);

        _stateMachine.OnTransitionRequested += HandleTransitionRequested;

        _stateMachine.Initialize(_states[EGameStateNet.Ready]);
        Server_SetState(EGameStateNet.Ready);

        UIManager.Instance?.Subscribe(this);
    }

    public override void FixedUpdateNetwork()
    {
        _stateMachine.Update();
    }

    private void HandleTransitionRequested(EGameStateNet nextNet)
    {
        if (!Runner.IsServer) return;

        if (_stateMachine.Current.NetState == EGameStateNet.PostGame)
            (_flowStrategy as OnlineGameFlow)?.ResetRound(); // 4/21

        _stateMachine.ChangeState(_states[nextNet]);
        Server_SetState(nextNet);
    }

    public void Server_SetState(EGameStateNet newState)
    {
        if (!Runner.IsServer) return;
        if (Current == newState) return;
        Previous = Current;
        Current = newState;
    }

    private static void OnStateChangedCallback(Changed<GameStateManager> changed)
    {
        Debug.Log($"State changed to {changed.Behaviour.Current}");
        changed.Behaviour.OnStateChanged?.Invoke(changed.Behaviour.Current);
    }

    private void OnDestroy()
    {
        if (_stateMachine != null)
            _stateMachine.OnTransitionRequested -= HandleTransitionRequested;

        UIManager.Instance?.Unsubscribe(this);
    }






}