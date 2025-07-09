using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WeaponHandler : NetworkBehaviour
{
    [Networked]
    public TickTimer FireLockTimer { get; set; } // �U�����b�N�p�̃^�C�}�[�@added 6/27
    public bool CanFire => FireLockTimer.ExpiredOrNotRunning(Runner); // �^�C�}�[�������Ă��Ȃ�������true


    [Header("Prefabs")] //���x�����łČ��₷���Ȃ邾��
    public GrenadeHandler grenadePrefab;

    //[Header("Effects")]
    //public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;

    //[Networked(OnChanged =nameof(OnFireChanged))]
    //public bool isFireing { get; set; }



    //�ǉ� 4/17
    [Header("throwAngle")]
    public float throwAngle = 30f;

    [Header("throwPower")]
    public float throwPower = 15f;

    // �v���C���[�̊����̋���
    public float inertiaFactor = 0.5f;

    //[Header("longPressTime")]
    //public float longPressThreshold = 0.5f;

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


    public override void FixedUpdateNetwork()
    {
        if (hpHandler.isDead)
            return;

        // Get the input from the network
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (CanFire && networkInputData.isGrenadeFireButtonPressed)
                FireGrenade(networkInputData.aimForwardVector);
        }
    }



    // Timing 
    TickTimer grenadeFireDelay = TickTimer.None;  //�A�C�e���{�b�N�X�Ȃ��̂�0����e�𐶐��ł���悤�Ɏ���

    void FireGrenade(Vector3 aimForwardVector)
    {
        if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            //string fakeNickname = "Guest"; // ��ł����ƃj�b�N�l�[���Ƃ��悤�ɂ��邻���ď����I�I�I�I�I�I�I�I�I�I�I�I�I

            Vector3 forwardDirection = aimForwardVector.normalized;

            // �v���C���[�� Transform ���擾
            Transform playerTransform = NetworkPlayer.ActivePlayers[Object.InputAuthority].transform;

            // �v���C���[�̃��[�J���E�����x�N�g��
            Vector3 playerRight = playerTransform.right;

            // �v���C���[�̑O�����x�N�g�����E�����ɉ�]�i������Ɋp�x������j
            Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, playerRight) * forwardDirection;

            // �v���C���[�̊�����ǉ�����
            Vector3 playerVelocity = networkCharacterController.Velocity;
            //Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, Vector3.right) * forwardDirection;
            Vector3 throwForce = (angleDirection * throwPower) + (playerVelocity * inertiaFactor);



            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(throwForce, Object.InputAuthority, networkObject, networkPlayer.nickName.ToString()); // aimForwardVector * 15 �� throwForce      networkObject added 4/23
            });



            // start a new timer to avoid grenade spamming
            grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

}
