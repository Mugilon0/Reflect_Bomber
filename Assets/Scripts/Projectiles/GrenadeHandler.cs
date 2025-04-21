using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GrenadeHandler : NetworkBehaviour
{
    public GameObject explosionParticleSystemPrefab;

    // Thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    // Timing
    TickTimer explodeTickTimer = TickTimer.None;

    // other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, string thrownByPlayerName)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();


        networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

        //　弾に回転を加える 4/18
        Vector3 frontFlip = transform.right * 10f; // 前転的な回転
        networkRigidbody.Rigidbody.angularVelocity = frontFlip;

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;

        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 4);
    }

    // Network update

    public override void FixedUpdateNetwork()
    {
        if (explodeTickTimer.Expired(Runner)) // タイマーが期限切れになったら実行される
        {
            Runner.Despawn(networkObject);

            // stop the explode timer from being triggered again
            explodeTickTimer = TickTimer.None;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //ネットワーク上の正しい位置に 補完もあるし別の位置にあるかもしれない

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity);

    }


}
