using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IGameState
{
    EGameStateNet NetState { get; }




    void OnEnter(IGameState prev);
    void OnExit(IGameState next);
    void OnUpdate();
    bool IsComplete();
}