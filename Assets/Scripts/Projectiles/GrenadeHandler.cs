using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;

public class GrenadeHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public Transform checkForImpackPoint;
    public LayerMask collisionLayers; //当たり判定用

    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    // Thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    NetworkObject thrownByNetworkObject; // 当たり判定? いらないかも

    // Timing
    TickTimer explodeTickTimer = TickTimer.None;

    // other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;



    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, NetworkObject thrownByNetworkObject, string thrownByPlayerName)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();


        networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

        //　弾に回転を加える 4/18
        Vector3 frontFlip = transform.right * 10f; // 前転的な回転
        networkRigidbody.Rigidbody.angularVelocity = frontFlip;

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;
        this.thrownByNetworkObject = thrownByNetworkObject;


        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 4);
    }

    // Network update

    public override void FixedUpdateNetwork()
    {
        //if (Object.HasInputAuthority) {}
        if (!Object.IsValid) return;

        if (explodeTickTimer.Expired(Runner))
        {
            explodeTickTimer = TickTimer.None;
            Explode();
            return;
        }


        // ボムに当たったかどうか判定 　Check if the rocket has hit anything
        int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, 0.5f, thrownByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX); // 近くに剛体がないかチェック

        //bool isValidHit = false;

        if (hitCount > 0)
        {
            Explode();
        }


        //// これは自分で生成したボムのダメージ食らわないようにするための処理
        //for (int i = 0; i < hitCount; i++)
        //{
        //    // check if we have hit a Hitbox
        //    if (hits[i].Hitbox != null)
        //    {
        //        // check that we didn't fire the rocket and hit ourselves. this can happen if the lag is a big high
        //        if (hits[i].Hitbox.Root.GetBehaviour<NetworkObject>() == thrownByNetworkObject) // 自分にヒットした場合
        //            isValidHit = false;
        //    }
        //}
    }

    private void Explode()
    {
        // サーバー上でのみ実行し、多重爆発を防ぐ
        if (!Object.HasStateAuthority || !Object.IsValid) return;

        // 爆発範囲内のプレイヤーにダメージを与える
        int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, 2, thrownByPlayerRef, hits, collisionLayers, HitOptions.None);

        for (int i = 0; i < hitCount; i++)
        {
            HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

            if (hpHandler != null && thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
            {
                NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                if (attackerPlayer != null)
                {
                    hpHandler.OnTakeDamage(attackerPlayer, 1);
                }
            }
        }

        // 爆発エフェクトを生成する (Despawnedから移動)
        //Instantiate(explosionParticleSystemPrefab, transform.position, Quaternion.identity);

        // グレネード自身をDespawnする
        Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //ネットワーク上の正しい位置に 補完もあるし別の位置にあるかもしれない

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity); // Explode()に変更したので不要 7/13

    }

    //public override void Despawned(NetworkRunner runner, bool hasState)
    //{
    //    Instantiate(explosionParticleSystemPrefab,checkForImpactPoint.position, Quaternion.identity);
    //}

}
