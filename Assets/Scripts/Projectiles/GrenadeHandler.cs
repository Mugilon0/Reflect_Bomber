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
    public LayerMask collisionLayers; //�����蔻��p

    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    // Thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    NetworkObject thrownByNetworkObject; // �����蔻��? ����Ȃ�����

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

        //�@�e�ɉ�]�������� 4/18
        Vector3 frontFlip = transform.right * 10f; // �O�]�I�ȉ�]
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

        if (explodeTickTimer.Expired(Runner)) // �^�C�}�[�������؂�ɂȂ�������s�����
        {

            Runner.Despawn(networkObject);


            // stop the explode timer from being triggered again
            explodeTickTimer = TickTimer.None;
            return;
        }

        // �{���ɓ����������ǂ������� �@Check if the rocket has hit anything
        int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, 0.5f, thrownByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX); // �߂��ɍ��̂��Ȃ����`�F�b�N

        bool isValidHit = false;

        if (hitCount > 0)
            isValidHit = true;

        for (int i = 0; i < hitCount; i++)
        {
            // check if we have hit a Hitbox
            if (hits[i].Hitbox != null)
            {
                // check that we didn't fire the rocket and hit ourselves. this can happen if the lag is a big high
                if (hits[i].Hitbox.Root.GetBehaviour<NetworkObject>() == thrownByNetworkObject) // �����Ƀq�b�g�����ꍇ
                    isValidHit = false;
            }
        }

        if (isValidHit)
        {
            // �����p�[�e�B�N���̓����蔻��
            hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, 2, thrownByPlayerRef, hits, collisionLayers, HitOptions.None);

            for (int i = 0; i < hitCount; i++)
            {
                // �͈͂ɂ��邷�ׂĂ�HPHandler�ɑ΂��Ď��s
                HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

                if (hpHandler != null)
                {
                    NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                    if (attackerPlayer != null)
                    {
                        hpHandler.OnTakeDamage(attackerPlayer, 1);
                    }
                }


            }

            Runner.Despawn(networkObject);
        }

    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //�l�b�g���[�N��̐������ʒu�� �⊮�����邵�ʂ̈ʒu�ɂ��邩������Ȃ�

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity);

    }

    //public override void Despawned(NetworkRunner runner, bool hasState)
    //{
    //    Instantiate(explosionParticleSystemPrefab,checkForImpactPoint.position, Quaternion.identity);
    //}

}
