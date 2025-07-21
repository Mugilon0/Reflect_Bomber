using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    // �e����̐��Ƃ��o���Ă���
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
            // ���͂ɉ����āA�K�؂Ȑ��Ƃɔ��˂𖽗߂���
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
