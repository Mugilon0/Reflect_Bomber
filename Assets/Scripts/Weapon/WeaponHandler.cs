using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WeaponHandler : NetworkBehaviour
{
    [Header("Prefabs")] //ラベルがでて見やすくなるだけ
    public GrenadeHandler grenadePrefab;

    //[Header("Effects")]
    //public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;


    //追加 4/17
    [Header("throwAngle")]
    public float throwAngle = 30f;

    [Header("throwPower")]
    public float throwPower = 15f;
    

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.isGrenadeFireButtonPressed)
                FireGrenade(networkInputData.aimForwardVector);
        }
    }


    // Timing 
    TickTimer grenadeFireDelay = TickTimer.None;  //アイテムボックスないので仮の実装

    void FireGrenade(Vector3 aimForwardVector)
    {
        if (grenadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            string fakeNickname = "Guest"; // 後でちゃんとニックネームとれるようにするそして消す！！！！！！！！！！！！！

            Vector3 forwardDirection = aimForwardVector.normalized;

            // プレイヤーの Transform を取得
            Transform playerTransform = NetworkPlayer.ActivePlayers[Object.InputAuthority].transform;

            // プレイヤーのローカル右方向ベクトル
            Vector3 playerRight = playerTransform.right;

            // プレイヤーの前方向ベクトルを右方向に回転（上方向に角度をつける）
            Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, playerRight) * forwardDirection;

            //Vector3 angleDirection = Quaternion.AngleAxis(-throwAngle, Vector3.right) * forwardDirection;
            Vector3 throwForce = angleDirection * throwPower;



            Runner.Spawn(grenadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GrenadeHandler>().Throw(throwForce, Object.InputAuthority, fakeNickname.ToString()); // aimForwardVector * 15 → throwForce
            });



            // start a new timer to avoid grenade spamming
            grenadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

}
