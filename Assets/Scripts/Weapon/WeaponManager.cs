using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    // 各武器の専門家を覚えておく
    private WeaponHandler weaponHandler;
    private LongWeaponHandler longWeaponHandler;

    private void Awake()
    {
        weaponHandler = GetComponent<WeaponHandler>();
        longWeaponHandler = GetComponent<LongWeaponHandler>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            // 入力に応じて、適切な専門家に発射を命令する
            if (weaponHandler != null)
            {
                weaponHandler.Fire(input);
            }

            if (longWeaponHandler != null)
            {
                longWeaponHandler.LongFire(input);
            }
        }
    }
}
