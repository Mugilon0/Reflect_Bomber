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

//    // ネットワーク同期されたプール本体
//    [Networked, Capacity(30)] // Capacityは予想される最大サイズ
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

//        // サーバー（ホスト）だけがプールを初期化する
//        if (!Object.HasStateAuthority) return;

//        InitializePool(shortRangeGrenadePrefab, shortRangePoolSize, shortRangePool);

//        InitializePool(longRangeGrenadePrefab, longRangePoolSize, longRangePool);

//    }


//    private void InitializePool(NetworkObject prefab, int size, NetworkLinkedList<NetworkObject> pool)
//    {
//        if (prefab == null) return;
//        // 指定された数だけグレネードを事前生成
//        for (int i = 0; i < size; i++)
//        {
//            NetworkObject spawnedGrenade = Runner.Spawn(prefab, Vector3.zero, Quaternion.identity, null, (runner, obj) =>
//            {
//                // 生成時にGrenadeHandlerにPoolManagerを教えてあげる
//                obj.GetComponent<GrenadeHandler>().SetPoolManager(this);
//            });

//            // 生成直後に非表示にし、プールに格納
//            spawnedGrenade.gameObject.SetActive(false);
//            pool.Add(spawnedGrenade);
//        }
//    }
//    // WeaponHandlerから呼び出されるメソッド
//    public void GetFromPool(Vector3 position, Quaternion rotation, Vector3 throwForce, PlayerRef ownerRef, NetworkObject ownerObj, string ownerName, GrenadeHandler.EBombType type)
//    {
//        NetworkLinkedList<NetworkObject> targetPool = (type == GrenadeHandler.EBombType.ShortRange) ? shortRangePool : longRangePool;


//        if (targetPool.Count == 0)
//        {
//            Debug.LogError("Pool is empty!");
//            return;
//        }

//        // プールから一番上のグレネードを取得
//        NetworkObject grenadeToActivate = targetPool[0];
//        targetPool.Remove(grenadeToActivate);

//        // RPC経由で全クライアントで有効化処理を行う
//        RPC_ActivateGrenade(grenadeToActivate, position, rotation, throwForce, ownerRef, ownerObj, ownerName, type);
//    }

//    // GrenadeHandlerから呼び出されるメソッド
//    public void ReturnToPool(NetworkObject grenade, GrenadeHandler.EBombType type)
//    {
//        // RPC経由で全クライアントで非表示化処理を行う
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

//        // サーバー側でのみプールに戻す
//        if (Object.HasStateAuthority)
//        {
//            NetworkLinkedList<NetworkObject> targetPool = (type == GrenadeHandler.EBombType.ShortRange) ? shortRangePool : longRangePool;
//            targetPool.Add(grenade);
//        }
//    }
//}