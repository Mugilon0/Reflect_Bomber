using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlineGameFlow : IGameFlow
{
    private readonly int _maxRounds; // ラウンド数
    private int _currentRound = 0; // 今のラウンド

    public int CurrentRound => _currentRound;

    public OnlineGameFlow(int maxRounds = 4)
    {
        _maxRounds = maxRounds;
    }

    public EGameStateNet DecideNext(EGameStateNet current, EGameStateNet previous)
    {
        switch (current)
        {
            case EGameStateNet.Lobby: return EGameStateNet.Loading;
            case EGameStateNet.Ready: return EGameStateNet.Loading;
            case EGameStateNet.Intro: return EGameStateNet.Game;
            case EGameStateNet.Game: return EGameStateNet.Outro;
            case EGameStateNet.PostGame: return EGameStateNet.Loading;

            case EGameStateNet.Outro:
                _currentRound++;
                return _currentRound < _maxRounds
                    ? EGameStateNet.Loading
                    : EGameStateNet.PostGame;

            case EGameStateNet.Loading:
                switch (previous)
                {
                    case EGameStateNet.Lobby: return EGameStateNet.Ready;
                    case EGameStateNet.Ready: return EGameStateNet.Intro;
                    case EGameStateNet.Outro: return EGameStateNet.Intro;
                    case EGameStateNet.PostGame: return EGameStateNet.Lobby;
                    default: return EGameStateNet.Lobby;
                }

            default: return EGameStateNet.Off;
        }
    }

    public void ResetRound()
    {
        _currentRound = 0;
    }
}