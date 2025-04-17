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

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;

        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 4);
    }

    // Network update

    public override void FixedUpdateNetwork()
    {
        if (explodeTickTimer.Expired(Runner)) // �^�C�}�[�������؂�ɂȂ�������s�����
        {
            Runner.Despawn(networkObject);

            // stop the explode timer from being triggered again
            explodeTickTimer = TickTimer.None;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //�l�b�g���[�N��̐������ʒu�� �⊮�����邵�ʂ̈ʒu�ɂ��邩������Ȃ�

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity);

    }


}
