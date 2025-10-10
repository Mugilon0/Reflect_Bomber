using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;

public class PutWeaponHandler : NetworkBehaviour
{
    [Header("Prefabs")] //���x�����łČ��₷���Ȃ邾��
    public GrenadeHandler grenadePrefab;

    [Header("PutPoint")]
    public Transform putPoint;


    // collisionLayers�͕ۗ�


    // other components
    HPHandler hpHandler;

    NetworkPlayer networkPlayer;
    NetworkObject networkObject;

    NetworkCharacterControllerPrototypeCustom networkCharacterController;


    private void Awake()
    {
        hpHandler = GetComponent<HPHandler>();
        // add 4/23
        networkPlayer = GetBehaviour<NetworkPlayer>();
        networkObject = GetComponent<NetworkObject>();

        networkCharacterController = GetComponent<NetworkCharacterControllerPrototypeCustom>();
    }



    // Timing 
    
    public void PutBomb()
    {
        //PutGrenade(input.aimForwardVector);

        Runner.Spawn(grenadePrefab, putPoint.position, putPoint.rotation, Object.InputAuthority, (runner, spawnedGrenade) =>
        {
            spawnedGrenade.GetComponent<GrenadeHandler>().Throw(Vector3.zero, Object.InputAuthority, networkObject, networkPlayer.nickName.ToString(), GrenadeHandler.EBombType.PutRange);
        });
    }

    //void PutGrenade(Vector3 aimForwardVector)
    //{

    //    Vector3 forwardDirection = aimForwardVector.normalized;

    //    // �v���C���[�� Transform ���擾
    //    Transform playerTransform = NetworkPlayer.ActivePlayers[Object.InputAuthority].transform;


    //    // �v���C���[�̊�����ǉ�����
    //    Vector3 playerVelocity = networkCharacterController.Velocity;

    //    Runner.Spawn(grenadePrefab, putPoint.position, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) => 
    //    {
    //        spawnedGrenade.GetComponent<GrenadeHandler>().Throw(throwForce.zero, Object.InputAuthority, networkObject, networkPlayer.nickName.ToString(), GrenadeHandler.EBombType.ShortRange);
    //    });


    //}


}