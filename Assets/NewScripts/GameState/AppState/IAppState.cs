using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAppState
{
    EAppState AppState { get; }
    void OnEnter(IAppState prev);
    void OnExit(IAppState next);
    void OnUpdate();
    bool IsComplete();
}