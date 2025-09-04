using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

[OrderAfter(typeof(NetworkCharacterControllerPrototypeCustom))]
public class PlayerAnimationHandler : NetworkBehaviour
{
    // �K�v�ȃR���|�[�l���g�ւ̎Q��
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    private NetworkCharacterControllerPrototypeCustom ncc;
    private HPHandler hpHandler;

    // �l�b�g���[�N���������v���p�e�B
    [Networked]
    private NetworkBool IsCharging { get; set; }

    [Networked(OnChanged = nameof(OnThrowStateChanged))]
    private NetworkBool DidThrow { get; set; }

    // MONOBEHAVIOUR

    void Start()
    {
        // �Q�[�����J�n���ꂽ����A�܂��̓I�u�W�F�N�g���L���ɂȂ�������ɌĂ΂��
        if (animator == null)
        {
            Debug.LogError($"[ANIMATION DEBUG] Start: Animator is NULL on {gameObject.name}. Prefab assignment might be missing!", gameObject);
        }
        else
        {
            Debug.Log($"[ANIMATION DEBUG] Start: Animator is assigned on {gameObject.name}.", gameObject);
        }
    }


    private void Awake()
    {
        //animator = GetComponentInChildren<Animator>();
        ncc = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
    }

    // NetworkBehaviour INTERFACE

    public override void FixedUpdateNetwork()
    {
        // ���͌���������v���C���[�i�������g�j�̓��͂��擾���A[Networked]�v���p�e�B���X�V
        if (GetInput(out NetworkInputData input))
        {
            IsCharging = input.IsCharging;

            // ��������͂��������ꍇ�ADidThrow�v���p�e�B�𔽓]������OnChanged�R�[���o�b�N���N��
            if (input.isShortThrow || input.isLongThrow)
            {
                DidThrow = !DidThrow;
            }
        }

        // Animator��IsDead�p�����[�^��HPHandler�̏�ԂƓ���
        if (hpHandler != null)
        {
            animator.SetBool("IsDead", hpHandler.isDead);
        }
    }

    public override void Render()
    {
        // Render���\�b�h�̍ŏ��Ƀ`�F�b�N��ǉ�
        if (animator == null)
        {
            Debug.LogError("Animator������܂���I�I");
            // �����Q�Ƃ������Ă�����A�ēx�擾�����݂�
            animator = GetComponentInChildren<Animator>();

            // ����ł�������Ȃ���΁A�G���[��h�����߂ɏ����𒆒f����
            if (animator == null)
            {
                Debug.LogError("Animator component could not be found on this object or its children!", gameObject);
                return;
            }
        }
        // --- �V�[���ɉ������A�j���[�V�������� ---
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Ready")
        {
            // Ready�V�[���ł͏��Speed��0�ɂ���Idle�A�j���[�V�����ɂ���
            animator.SetFloat("Speed", 0);
            return;
        }

        // --- �o�g���V�[���ł̃A�j���[�V�������� ---

        // Speed�p�����[�^�̍X�V
        Vector3 horizontalVelocity = ncc.Velocity;
        horizontalVelocity.y = 0;
        animator.SetFloat("Speed", horizontalVelocity.magnitude);

        // IsCharging�p�����[�^�̍X�V
        animator.SetBool("IsCharging", IsCharging);
    }

    // DidThrow�v���p�e�B���ύX���ꂽ�Ƃ��ɑS�N���C�A���g�ŌĂ΂��
    static void OnThrowStateChanged(Changed<PlayerAnimationHandler> changed)
    {
        // ���S���Ă��Ȃ���΁AThrow�g���K�[������
        if (changed.Behaviour.hpHandler != null && !changed.Behaviour.hpHandler.isDead)
        {
            changed.Behaviour.animator.SetTrigger("Throw");
        }
    }
}