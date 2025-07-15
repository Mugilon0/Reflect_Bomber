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

    // �� 1. �Փ˔����̍��}�ƂȂ�u���v��ǉ�
    [Networked]
    private NetworkBool IsCollisionExplosionQueued { get; set; }

    // �� 2. ���d������h�����߂́u�L���t���O�v���ǉ�
    private bool hasExploded = false;


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
        if (!Object.IsValid) return;

        if (explodeTickTimer.Expired(Runner))
        {
            explodeTickTimer = TickTimer.None;
            Explode();
            return;
        }

        // �� 3. �Փ˂́u���v�������Ă����甚��
        if (IsCollisionExplosionQueued)
        {
            Explode();
            return;
        }

        // �{���ɓ����������ǂ������� �@Check if the rocket has hit anything
        int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, 0.5f, thrownByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX); // �߂��ɍ��̂��Ȃ����`�F�b�N

        //bool isValidHit = false;

        if (hitCount > 0)
        {
            Explode();
        }


        //// ����͎����Ő��������{���̃_���[�W�H���Ȃ��悤�ɂ��邽�߂̏���
        //for (int i = 0; i < hitCount; i++)
        //{
        //    // check if we have hit a Hitbox
        //    if (hits[i].Hitbox != null)
        //    {
        //        // check that we didn't fire the rocket and hit ourselves. this can happen if the lag is a big high
        //        if (hits[i].Hitbox.Root.GetBehaviour<NetworkObject>() == thrownByNetworkObject) // �����Ƀq�b�g�����ꍇ
        //            isValidHit = false;
        //    }
        //}
    }

    private void Explode()
    {
        // �� 4. ���d�������m���ɖh��
        if (hasExploded) return;
        hasExploded = true;

        // �T�[�o�[��ł̂ݎ��s���A���d������h��
        if (!Object.HasStateAuthority || !Object.IsValid) return;

        // ������ PlayerRef.None ���g���A�u�ǂ̃v���C���[���������Ȃ��v���w�肷�� ������
        int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, 3, PlayerRef.None, hits, ~0, HitOptions.None);

        // ���o���ꂽ�S�ẴI�u�W�F�N�g�ihitCount���j�����[�v�Ń`�F�b�N����
        for (int i = 0; i < hitCount; i++)
        {
            // --- �܂��A�v���C���[�ɓ����������`�F�b�N ---
            if (hits[i].Hitbox != null)
            {
                HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();
                if (hpHandler != null)
                {
                    if (thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
                    {
                        // �����͖h��
                        //if (hpHandler.Object != thrownByNetworkObject)
                        //{
                        NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                        if (attackerPlayer != null)
                        {
                            // �������U���ҏ���n���ă_���[�W�����ƃX�R�A���Z���s��
                            hpHandler.OnTakeDamage(attackerPlayer, 1);
                        }
                        //}
                    }
                }
            }

            // --- ���ɁA���̃{���ɓ����������`�F�b�N ---
            if (hits[i].Collider != null)
            {
                GrenadeHandler otherGrenade = hits[i].Collider.GetComponentInParent<GrenadeHandler>();
                if (otherGrenade != null && otherGrenade != this)
                {
                    // ���̃O���l�[�h��U��������
                    Debug.Log("�܂Ƃ߂Ĕ��������܂�");
                    otherGrenade.Explode();
                }
            }
        }

        // �O���l�[�h���g��Despawn����
        Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>(); //�l�b�g���[�N��̐������ʒu�� �⊮�����邵�ʂ̈ʒu�ɂ��邩������Ȃ�

        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity); // Explode()�ɕύX�����̂ŕs�v 7/13

    }

    //public override void Despawned(NetworkRunner runner, bool hasState)
    //{
    //    Instantiate(explosionParticleSystemPrefab,checkForImpactPoint.position, Quaternion.identity);
    //}
    private void OnCollisionEnter(Collision collision)
    {
        // �T�[�o�[�iState Authority�����v���C���[�j�ȊO�͉������Ȃ�
        if (!Object.HasStateAuthority)
        {
            return;
        }

        // �Ԃ��������肪�ʂ̃O���l�[�h���ǂ������`�F�b�N����
        if (collision.gameObject.TryGetComponent<GrenadeHandler>(out var otherGrenade))
        {
            // �� 5. Explode()�𒼐ڌĂ΂��A�u���v�𗧂Ă邾���ɂ���
            IsCollisionExplosionQueued = true;
            // ���肪�O���l�[�h�Ȃ�A�������g�𑦍��ɔ���������
            // �i����̃O���l�[�h�����l�ɂ��̏������Ăяo�����߁A��������������j
            //Explode();
        }
    }

}