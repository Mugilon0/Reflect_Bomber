using System;
using System.Collections.Generic;

public class StateMachine<T> where T : Enum
{
    public class StateHooks
    {
        public Action<T> onEnter; // ���̏�Ԃɓ��������ɌĂ΂��A�N�V����
        public Action<T> onExit;  // ���̏�Ԃ���o�鎞�ɌĂ΂��A�N�V����
        public Action onUpdate;  // ���̏�Ԃ��A�N�e�B�u�ȊԁA���t���[���Ă΂��A�N�V����
    }

    private Dictionary<T, StateHooks> _states = new Dictionary<T, StateHooks>();
    private (T current, T previous) _previousFrameStates = (default(T), default(T)); // �O�̃t���[���̏�Ԃ�ێ�

    // �C���f�N�T�[���g���� stateMachine[EGameState.Intro].onEnter = ... �̂悤�ɃA�N�Z�X�ł���悤�ɂ���
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
        // ���݃A�N�e�B�u�ȏ�Ԃ� onUpdate ���W�b�N�����s
        if (_states.TryGetValue(_previousFrameStates.current, out StateHooks currentHooks))
        {
            currentHooks.onUpdate?.Invoke();
        }

        // ��Ԃ��ω����Ă��Ȃ���Ή������Ȃ�
        if (Equals(currentState, _previousFrameStates.current) && Equals(previousState, _previousFrameStates.previous))
        {
            return;
        }

        // �O�̏�Ԃ� onExit ���W�b�N�����s
        if (!Equals(previousState, default(T)) && _states.TryGetValue(previousState, out StateHooks previousHooks))
        {
            previousHooks.onExit?.Invoke(currentState); // ���̏�Ԃ������Ƃ��ēn��
        }

        // �V������Ԃ� onEnter ���W�b�N�����s
        if (!Equals(currentState, default(T)) && _states.TryGetValue(currentState, out StateHooks nextHooks))
        {
            nextHooks.onEnter?.Invoke(previousState); // �O�̏�Ԃ������Ƃ��ēn��
        }

        _previousFrameStates = (currentState, previousState); // ���݂̏�Ԃ����̃t���[���̂��߂ɕۑ�
    }
}