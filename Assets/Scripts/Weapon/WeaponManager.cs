using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    // �e����̐��Ƃ��o���Ă���
    private WeaponHandler weaponHandler;
    private LongWeaponHandler longWeaponHandler;


    private HPHandler hpHandler;

    [Networked]
    private TickTimer fireCooldownTimer { get; set; } // ���틤�ʂ̃N�[���_�E���^�C�}�[

    public void LockWeapons(float duration)
    {
        fireCooldownTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    private void Awake()
    {
        weaponHandler = GetComponent<WeaponHandler>();
        longWeaponHandler = GetComponent<LongWeaponHandler>();

        hpHandler = GetComponent<HPHandler>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!fireCooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        if (hpHandler != null && hpHandler.isDead)
        {
            return;
        }


        if (GetInput(out NetworkInputData input))
        {
            bool hasFired = false;

            // ���͂ɉ����āA�K�؂Ȑ��Ƃɔ��˂𖽗߂���
            if (input.isShortThrow && weaponHandler != null)
            {
                weaponHandler.Fire(input);
                hasFired = true;
            }

            if (input.isLongThrow && longWeaponHandler != null)
            {
                longWeaponHandler.LongFire(input);
                hasFired = true;
            }


            if (hasFired)
            {
                // �����Ŕ��ˌ�̃N�[���_�E�����Ԃ�ݒ�i1.0�b�j
                fireCooldownTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
            }
        }
    }
}
