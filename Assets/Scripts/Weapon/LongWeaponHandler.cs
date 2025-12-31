using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class LongWeaponHandler : NetworkBehaviour
{
    //[Networked]
    //public TickTimer FireLockTimer { get; set; } // 攻撃ロック用のタイマー added 6/27
    //public bool CanFire => FireLockTimer.ExpiredOrNotRunning(Runner); // タイマーが動いていない時だけtrue


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


    [Header("throwAngle")]
    public float throwAngle = 30f;

    [Header("throwPower - Charge Settings")]
    public float minThrowPower = 30f;   // 最小パワー（0.3秒チャージ時）
    public float maxThrowPower = 45f;   // 最大パワー（最大チャージ時）
    public float minChargeTime = 0.3f;  // LongBomb判定の閾値（CharacterInputHandlerと同じ値）
    public float maxChargeTime = 1.3f;  // 最大チャージ時間（これ以上溜めても45のまま）

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


    //public override void FixedUpdateNetwork()
    //{
    //    if (hpHandler.isDead)
    //        return;

    //    // Get the input from the network
    //    if (GetInput(out NetworkInputData networkInputData))
    //    {
    //        if (CanFire && networkInputData.isGrenadeFireButtonPressed)
    //            FireLongGrenade(networkInputData.aimForwardVector);
    //    }
    //}



    // Timing 
    TickTimer grenadeFireDelay = TickTimer.None;  //アイテムボックスないので0から弾を生成できるように実装

    public void LongFire(NetworkInputData input)
    {
        // チャージ時間からパワーを計算
        // 0.3秒 → 30, 1.3秒以上 → 45
        float chargeRatio = Mathf.InverseLerp(minChargeTime, maxChargeTime, input.longThrowCharge);
        float power = Mathf.Lerp(minThrowPower, maxThrowPower, chargeRatio);

        FireLongGrenade(input.aimForwardVector, power);
    }

    void FireLongGrenade(Vector3 aimForwardVector, float throwPowerValue)
    {
        //if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        //{
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
        Vector3 throwForce = (angleDirection * throwPowerValue) + (playerVelocity * inertiaFactor);



        if (GrenadePoolManager.Instance != null)
        {
            Vector3 spawnPosition = aimPoint.position + aimForwardVector * 0.5f;
            Quaternion spawnRotation = Quaternion.LookRotation(aimForwardVector);

            GrenadePoolManager.Instance.GetFromPool(
                spawnPosition,
                spawnRotation,
                throwForce,
                Object.InputAuthority,
                networkObject,
                networkPlayer.nickName.ToString(),
                GrenadeHandler.EBombType.LongRange
            );
        }
        else
        {
            // フォールバック: プールがない場合は従来の方法（非推奨）
            Debug.LogWarning("GrenadePoolManager not found! Using legacy Spawn method.");

            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 0.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(throwForce, Object.InputAuthority, networkObject, networkPlayer.nickName.ToString(), GrenadeHandler.EBombType.LongRange);
            });
        }

        // start a new timer to avoid grenade spamming
        //grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        //}
    }

}