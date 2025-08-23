using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.VisualScripting;
using System;


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

    // ★ 1. 衝突爆発の合図となる「旗」を追加
    [Networked]
    private NetworkBool IsCollisionExplosionQueued { get; set; }

    // ★ 2. 多重爆発を防ぐための「記憶フラグ」も追加
    private bool hasExploded = false;

    public enum EBombType
    {
        ShortRange,
        LongRange
    }
    [Networked] private EBombType bombType { get; set; }

    private float explosionRadius;


    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, NetworkObject thrownByNetworkObject, string thrownByPlayerName, EBombType type)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        bombType = type;

        switch(bombType)
        {
            case EBombType.ShortRange:
                explosionRadius = 1.0f;
                break;
            case EBombType.LongRange:
                explosionRadius = 4.0f;
                break;
        }


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

        // ★ 3. 衝突の「旗」が立っていたら爆発
        if (IsCollisionExplosionQueued)
        {
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
        // ★ 4. 多重爆発を確実に防ぐ
        if (hasExploded) return;
        hasExploded = true;

        // サーバー上でのみ実行し、多重爆発を防ぐ
        if (!Object.HasStateAuthority || !Object.IsValid) return;

        // ★★★ PlayerRef.None を使い、「どのプレイヤーも無視しない」を指定する ★★★
        int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, explosionRadius, PlayerRef.None, hits, ~0, HitOptions.None);

        // 検出された全てのオブジェクト（hitCount分）をループでチェックする
        for (int i = 0; i < hitCount; i++)
        {
            // --- まず、プレイヤーに当たったかチェック ---
            if (hits[i].Hitbox != null)
            {
                HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();
                if (hpHandler != null)
                {
                    if (thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
                    {
                        // 自爆は防ぐ
                        //if (hpHandler.Object != thrownByNetworkObject)
                        //{
                        NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                        if (attackerPlayer != null)
                        {
                            // 正しい攻撃者情報を渡してダメージ処理とスコア加算を行う
                            hpHandler.OnTakeDamage(attackerPlayer, 1);
                        }
                        //}
                    }
                }
            }

            // --- 次に、他のボムに当たったかチェック ---
            if (hits[i].Collider != null)
            {
                GrenadeHandler otherGrenade = hits[i].Collider.GetComponentInParent<GrenadeHandler>();
                if (otherGrenade != null && otherGrenade != this)
                {
                    // 他のグレネードを誘爆させる
                    Debug.Log("まとめて爆発させます");
                    otherGrenade.Explode();
                }
            }
        }

        // グレネード自身をDespawnする
        Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //ネットワーク上の正しい位置に 補完もあるし別の位置にあるかもしれない

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity); // Explode()に変更したので不要 7/13

        if (bombType == EBombType.ShortRange)
        {
            AudioManager.Play("StrawberryExplosionSFX", AudioManager.MixerTarget.SFX, grenadeMesh.transform.position);
        }
        else // EBombType.LongRange
        {
            AudioManager.Play("OrangeExplosionSFX", AudioManager.MixerTarget.SFX, grenadeMesh.transform.position);
        }
    }

    //public override void Despawned(NetworkRunner runner, bool hasState)
    //{
    //    Instantiate(explosionParticleSystemPrefab,checkForImpactPoint.position, Quaternion.identity);
    //}
    private void OnCollisionEnter(Collision collision)
    {
        if (Object == null) return;
        if (hasExploded) return;

        // サーバー（State Authorityを持つプレイヤー）以外は何もしない
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (bombType == EBombType.LongRange)
        {
            // "Default" レイヤーは、多くのUnityプロジェクトで地面や壁に使われる標準的なレイヤーです
            if (collision.gameObject.CompareTag("Ground"))
            {
                IsCollisionExplosionQueued = true;
                return; // 地面に触れたら即爆発させるので、以降の処理は不要
            }
        }

        // ぶつかった相手が別のグレネードかどうかをチェックする
        if (collision.gameObject.TryGetComponent<GrenadeHandler>(out var otherGrenade))
        {
            // ★ 5. Explode()を直接呼ばず、「旗」を立てるだけにする
            IsCollisionExplosionQueued = true;
            // 相手がグレネードなら、自分自身を即座に爆発させる
            // （相手のグレネードも同様にこの処理を呼び出すため、両方が爆発する）
            //Explode();
        }
    }

}

