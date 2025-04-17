using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WeaponHandler : NetworkBehaviour
{
    [Header("Prefabs")] //���x�����łČ��₷���Ȃ邾��
    public GrenadeHandler grenadePrefab;

    //[Header("Effects")]
    //public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;





    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.isGrenadeFireButtonPressed)
                FireGrenade(networkInputData.aimForwardVector);
        }
    }


    // Timing 
    TickTimer grenadeFireDelay = TickTimer.None;  //�A�C�e���{�b�N�X�Ȃ��̂ŉ��̎���

    void FireGrenade(Vector3 aimForwardVector)
    {
        if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            string fakeNickname = "Guest"; // ��ł����ƃj�b�N�l�[���Ƃ��悤�ɂ��邻���ď����I�I�I�I�I�I�I�I�I�I�I�I�I
            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(aimForwardVector * 15, Object.InputAuthority, fakeNickname.ToString());
            });



            // start a new timer to avoid grenade spamming
            grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

}
