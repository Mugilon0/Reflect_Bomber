//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;
//using Unity.VisualScripting;
//using System;


//public class GrenadePoolManager : NetworkBehaviour
//{
//    [Header("Prefabs")]
//    [SerializeField] public NetworkObject shortRangeGrenadePrefab;
//    [SerializeField] public NetworkObject longRangeGrenadePrefab;

//    [Header("Pool SIzes")]
//    [SerializeField] private int shortRangePoolSize = 20;
//    [SerializeField] private int longRangePoolSize = 20;

//    // �l�b�g���[�N�������ꂽ�v�[���{��
//    [Networked, Capacity(30)] // Capacity�͗\�z�����ő�T�C�Y
//    private NetworkLinkedList<NetworkObject> shortRangePool { get; }

//    [Networked, Capacity(30)]
//    private NetworkLinkedList<NetworkObject> longRangePool { get; }


//    public static GrenadePoolManager Instance { get; private set; }

//    public override void Spawned()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        base.Spawned();

//        // �T�[�o�[�i�z�X�g�j�������v�[��������������
//        if (!Object.HasStateAuthority) return;

//        InitializePool(shortRangeGrenadePrefab, shortRangePoolSize, shortRangePool);

//        InitializePool(longRangeGrenadePrefab, longRangePoolSize, longRangePool);

//    }


//    private void InitializePool(NetworkObject prefab, int size, NetworkLinkedList<NetworkObject> pool)
//    {
//        if (prefab == null) return;
//        // �w�肳�ꂽ�������O���l�[�h�����O����
//        for (int i = 0; i < size; i++)
//        {
//            NetworkObject spawnedGrenade = Runner.Spawn(prefab, Vector3.zero, Quaternion.identity, null, (runner, obj) =>
//            {
//                // ��������GrenadeHandler��PoolManager�������Ă�����
//                obj.GetComponent<GrenadeHandler>().SetPoolManager(this);
//            });

//            // ��������ɔ�\���ɂ��A�v�[���Ɋi�[
//            spawnedGrenade.gameObject.SetActive(false);
//            pool.Add(spawnedGrenade);
//        }
//    }
//    // WeaponHandler����Ăяo����郁�\�b�h
//    public void GetFromPool(Vector3 position, Quaternion rotation, Vector3 throwForce, PlayerRef ownerRef, NetworkObject ownerObj, string ownerName, GrenadeHandler.EBombType type)
//    {
//        NetworkLinkedList<NetworkObject> targetPool = (type == GrenadeHandler.EBombType.ShortRange) ? shortRangePool : longRangePool;


//        if (targetPool.Count == 0)
//        {
//            Debug.LogError("Pool is empty!");
//            return;
//        }

//        // �v�[�������ԏ�̃O���l�[�h���擾
//        NetworkObject grenadeToActivate = targetPool[0];
//        targetPool.Remove(grenadeToActivate);

//        // RPC�o�R�őS�N���C�A���g�ŗL�����������s��
//        RPC_ActivateGrenade(grenadeToActivate, position, rotation, throwForce, ownerRef, ownerObj, ownerName, type);
//    }

//    // GrenadeHandler����Ăяo����郁�\�b�h
//    public void ReturnToPool(NetworkObject grenade, GrenadeHandler.EBombType type)
//    {
//        // RPC�o�R�őS�N���C�A���g�Ŕ�\�����������s��
//        RPC_DeactivateGrenade(grenade, type);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    private void RPC_ActivateGrenade(NetworkObject grenade, Vector3 position, Quaternion rotation, Vector3 throwForce, PlayerRef ownerRef, NetworkObject ownerObj, string ownerName, GrenadeHandler.EBombType type)
//    {
//        GrenadeHandler grenadeHandler = grenade.GetComponent<GrenadeHandler>();
//        grenadeHandler.ResetState();

//        grenade.transform.position = position;
//        grenade.transform.rotation = rotation;

//        grenadeHandler.Throw(throwForce, ownerRef, ownerObj, ownerName, type);

//        grenade.gameObject.SetActive(true);


//        //grenadeHandler.Throw(throwForce, ownerRef, ownerObj, ownerName, type);
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    private void RPC_DeactivateGrenade(NetworkObject grenade, GrenadeHandler.EBombType type)
//    {
//        grenade.gameObject.SetActive(false);

//        // �T�[�o�[���ł̂݃v�[���ɖ߂�
//        if (Object.HasStateAuthority)
//        {
//            NetworkLinkedList<NetworkObject> targetPool = (type == GrenadeHandler.EBombType.ShortRange) ? shortRangePool : longRangePool;
//            targetPool.Add(grenade);
//        }
//    }
//}