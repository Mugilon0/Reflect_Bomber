using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    // 各武器の専門家を覚えておく
    private WeaponHandler weaponHandler;
    private LongWeaponHandler longWeaponHandler;


    private HPHandler hpHandler;

    [Networked]
    private TickTimer fireCooldownTimer { get; set; } // 武器共通のクールダウンタイマー

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

            // 入力に応じて、適切な専門家に発射を命令する
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
                // ここで発射後のクールダウン時間を設定（1.0秒）
                fireCooldownTimer = TickTimer.CreateFromSeconds(Runner, 1.0f);
            }
        }
    }
}
