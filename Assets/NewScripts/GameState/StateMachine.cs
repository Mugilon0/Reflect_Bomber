using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StateMachine
{
    private IGameState _current;
    private IGameState _previous;
    private readonly IGameFlow _flow;

    public IGameState Current => _current;
    public IGameState Previous => _previous;

    // GameStateManagerÇ™çwì«Ç∑ÇÈ
    public event Action<EGameStateNet> OnTransitionRequested;

    public StateMachine(IGameFlow flow)
    {
        _flow = flow;
    }

    public void Initialize(IGameState initialState)
    {
        _current = initialState;
        _current.OnEnter(null);
    }

    public void Update()
    {
        if (_current == null) return;

        _current.OnUpdate();

        if (_current.IsComplete())
        {
            EGameStateNet nextNet = _flow.DecideNext(
                _current.NetState,
                _previous?.NetState ?? EGameStateNet.Off
            );
            OnTransitionRequested?.Invoke(nextNet);
        }
    }

    public void ChangeState(IGameState next)
    {
        _current.OnExit(next);
        _previous = _current;
        _current = next;
        _current.OnEnter(_previous);
    }
}
