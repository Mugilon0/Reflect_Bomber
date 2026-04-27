using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IGameFlowStrategy.cs
public interface IGameFlow
{
    EGameStateNet DecideNext(EGameStateNet current, EGameStateNet previous);
}
