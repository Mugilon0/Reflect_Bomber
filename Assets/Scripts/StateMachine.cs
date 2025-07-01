using System;
using System.Collections.Generic;

public class StateMachine<T> where T : Enum
{
    public class StateHooks
    {
        public Action<T> onEnter; // この状態に入った時に呼ばれるアクション
        public Action<T> onExit;  // この状態から出る時に呼ばれるアクション
        public Action onUpdate;  // この状態がアクティブな間、毎フレーム呼ばれるアクション
    }

    private Dictionary<T, StateHooks> _states = new Dictionary<T, StateHooks>();
    private (T current, T previous) _previousFrameStates = (default(T), default(T)); // 前のフレームの状態を保持

    // インデクサーを使って stateMachine[EGameState.Intro].onEnter = ... のようにアクセスできるようにする
    public StateHooks this[T statename]
    {
        get
        {
            if (!_states.TryGetValue(statename, out StateHooks state))
            {
                state = new StateHooks();
                _states[statename] = state;
            }
            return state;
        }
    }

    public void Update(T currentState, T previousState)
    {
        // 現在アクティブな状態の onUpdate ロジックを実行
        if (_states.TryGetValue(_previousFrameStates.current, out StateHooks currentHooks))
        {
            currentHooks.onUpdate?.Invoke();
        }

        // 状態が変化していなければ何もしない
        if (Equals(currentState, _previousFrameStates.current) && Equals(previousState, _previousFrameStates.previous))
        {
            return;
        }

        // 前の状態の onExit ロジックを実行
        if (!Equals(previousState, default(T)) && _states.TryGetValue(previousState, out StateHooks previousHooks))
        {
            previousHooks.onExit?.Invoke(currentState); // 次の状態を引数として渡す
        }

        // 新しい状態の onEnter ロジックを実行
        if (!Equals(currentState, default(T)) && _states.TryGetValue(currentState, out StateHooks nextHooks))
        {
            nextHooks.onEnter?.Invoke(previousState); // 前の状態を引数として渡す
        }

        _previousFrameStates = (currentState, previousState); // 現在の状態を次のフレームのために保存
    }
}