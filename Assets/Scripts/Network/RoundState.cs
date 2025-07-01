using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
//using Helpers.Linq;

public class RoundManager : NetworkBehaviour
{
    //[Networked] // (OnChanged = nameof(OnSessionConfigChanged))�͂������񖳎��@�Q�[�����Ԃ̑I����
    //public int MaxTimeIndex { get; set; }
    public static float MaxTime => 120.0f;


    [Networked]
    public int TickStarted { get; set; }
    public static float Time => Instance?.Object?.IsValid == true // ���E���h�̌o�ߎ��Ԃ��v�Z���� ����GameManager���l�b�g���[�N��ŗL���Ȃ�A����Ƀ^�C�}�[���J�n�ς݂��`�F�b�N����B�J�n�ς݂Ȃ�**�o�ߎ��ԁi�b�j**���v�Z���ĕԂ��A�܂��Ȃ�0��Ԃ��B��������GameManager���L���łȂ���΁A�ⓚ���p��0��Ԃ��B
        ? (Instance.TickStarted == 0
            ? 0
            : (Instance.Runner.Simulation.Tick - Instance.TickStarted) * Instance.Runner.Simulation.DeltaTime)
        : 0;

    public static RoundManager Instance { get; private set; }

    public override void Spawned()
    {
        // �V���O���g���C���X�^���X�Ƃ��Ď��g��ݒ�
        if (Instance == null)
        {
            Instance = this;
            // �V�[�����܂����ő��݂�����
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // ���ɃC���X�^���X�����݂���ꍇ�́A�d�����Ȃ��悤�Ɏ��g��j������
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // �j�������ۂɁA�V���O���g���C���X�^���X���N���A����
        if (Instance == this)
        {
            Instance = null;
            //InterfaceManager.Instance.resultsScreen.Clear(); �������@���邩�킩��Ȃ�
        }
    }


    //static void OnSessionConfigChanged(Changed<GameManager> changed)
    //{
    //    InterfaceManager.Instance.sessionScreen.UpdateSessionConfig();
    //}  // �Q�[�����Ԃ��v���C���[�����߂�킯�ł͂Ȃ��̂ŏȗ�







}
