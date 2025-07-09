using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WeaponHandler : NetworkBehaviour
{
    [Networked]
    public TickTimer FireLockTimer { get; set; } // 攻撃ロック用のタイマー　added 6/27
    public bool CanFire => FireLockTimer.ExpiredOrNotRunning(Runner); // タイマーが動いていない時だけtrue


    [Header("Prefabs")] //ラベルがでて見やすくなるだけ
    public GrenadeHandler grenadePrefab;

    //[Header("Effects")]
    //public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;

    //[Networked(OnChanged =nameof(OnFireChanged))]
    //public bool isFireing { get; set; }



    //追加 4/17
    [Header("throwAngle")]
    public float throwAngle = 30f;

    [Header("throwPower")]
    public float throwPower = 15f;

    // プレイヤーの慣性の強さ
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
    TickTimer grenadeFireDelay = TickTimer.None;  //アイテムボックスないので0から弾を生成できるように実装

    void FireGrenade(Vector3 aimForwardVector)
    {
        if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            //string fakeNickname = "Guest"; // 後でちゃんとニックネームとれるようにするそして消す！！！！！！！！！！！！！

            Vector3 forwardDirection = aimForwardVector.normalized;

            // プレイヤーの Transform を取得
            Transform playerTransform = NetworkPlayer.ActivePlayers[Object.InputAuthority].transform;

            // プレイヤーのローカル右方向ベクトル
            Vector3 playerRight = playerTransform.right;

            // プレイヤーの前方向ベクトルを右方向に回転（上方向に角度をつける）
            Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, playerRight) * forwardDirection;

            // プレイヤーの慣性を追加する
            Vector3 playerVelocity = networkCharacterController.Velocity;
            //Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, Vector3.right) * forwardDirection;
            Vector3 throwForce = (angleDirection * throwPower) + (playerVelocity * inertiaFactor);



            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(throwForce, Object.InputAuthority, networkObject, networkPlayer.nickName.ToString()); // aimForwardVector * 15 → throwForce      networkObject added 4/23
            });



            // start a new timer to avoid grenade spamming
            grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

}
