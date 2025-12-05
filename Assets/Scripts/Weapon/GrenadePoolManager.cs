using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Fusion1対応のグレネードプール（ワープ問題を解決した最適化版）
/// </summary>
public class GrenadePoolManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] public NetworkObject shortRangeGrenadePrefab;
    [SerializeField] public NetworkObject longRangeGrenadePrefab;
    [SerializeField] public NetworkObject putRangeGrenadePrefab;

    [Header("Pool Sizes")]
    [SerializeField] private int shortRangePoolSize = 20;
    [SerializeField] private int longRangePoolSize = 20;
    [SerializeField] private int putRangePoolSize = 10;
    [SerializeField] private int maxPoolSize = 60;


    //[Header("Effect Pool Settings")]
    //[SerializeField] private int effectPoolSize = 10;

    // Queueを使用してFIFO方式で管理
    private Queue<NetworkObject> shortRangePool = new Queue<NetworkObject>();
    private Queue<NetworkObject> longRangePool = new Queue<NetworkObject>();
    private Queue<NetworkObject> putRangePool = new Queue<NetworkObject>();

    // 11/20
    //private List<ParticleSystem> shortEffectPool = new List<ParticleSystem>();
    //private List<ParticleSystem> longEffectPool = new List<ParticleSystem>();
    //private List<ParticleSystem> putEffectPool = new List<ParticleSystem>();
    //private Transform poolParent;

    private HashSet<NetworkObject> activeGrenades = new HashSet<NetworkObject>();

    public static GrenadePoolManager Instance { get; private set; }

    private bool hasInitialized = false;
    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate GrenadePoolManager detected. Keeping original. HasStateAuthority: {Object.HasStateAuthority}");

            // 重複している場合は即座に削除
            if (Object.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
            return;
        }
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"GrenadePoolManager Instance set. HasStateAuthority: {Object.HasStateAuthority}");
        }


        base.Spawned();

        if (hasInitialized)
        {
            Debug.LogWarning("GrenadePoolManager already initialized.スキップします.");
            return;
        }


        // サーバーだけがプールを初期化
        if (Object.HasStateAuthority)
        {
            InitializeGrenadePool(shortRangeGrenadePrefab, shortRangePoolSize, shortRangePool);
            InitializeGrenadePool(longRangeGrenadePrefab, longRangePoolSize, longRangePool);
            InitializeGrenadePool(putRangeGrenadePrefab, putRangePoolSize, putRangePool);
        }

        // 2. 全員: エフェクトプールの初期化 (3種類それぞれ作成)
        //InitializeEffectPools();

        hasInitialized = true;
    }


    //private void InitializeEffectPools()
    //{
    //    // 各ボムのPrefabから、設定されているエフェクトPrefabを抜き出してプールを作成
    //    CreateEffectPoolFromGrenade(shortRangeGrenadePrefab, shortEffectPool);
    //    CreateEffectPoolFromGrenade(longRangeGrenadePrefab, longEffectPool);
    //    CreateEffectPoolFromGrenade(putRangeGrenadePrefab, putEffectPool);
    //}

    //private void CreateEffectPoolFromGrenade(NetworkObject grenadePrefab, List<ParticleSystem> pool)
    //{
    //    if (grenadePrefab == null) return;

    //    // ボムのPrefabに付いているHandlerを取得
    //    var handler = grenadePrefab.GetComponent<GrenadeHandler>();
    //    if (handler == null || handler.explosionParticleSystemPrefab == null)
    //    {
    //        Debug.LogWarning($"Effect prefab missing on {grenadePrefab.name}");
    //        return;
    //    }

    //    GameObject effectPrefab = handler.explosionParticleSystemPrefab;

    //    for (int i = 0; i < effectPoolSize; i++)
    //    {
    //        GameObject obj = Instantiate(effectPrefab, poolParent);
    //        obj.SetActive(false);
    //        var ps = obj.GetComponent<ParticleSystem>();
    //        if (ps != null)
    //        {
    //            pool.Add(ps);
    //        }
    //        else
    //        {
    //            // ParticleSystemがルートにない場合、子から探すか、運用でカバーする
    //            Debug.LogWarning($"ParticleSystem component missing on root of {effectPrefab.name}");
    //        }
    //    }
    //}



    //public void PlayExplosionEffect(GrenadeHandler.EBombType type, Vector3 position)
    //{
    //    List<ParticleSystem> targetPool = null;

    //    // タイプに応じてプールを切り替え
    //    switch (type)
    //    {
    //        case GrenadeHandler.EBombType.ShortRange: targetPool = shortEffectPool; break;
    //        case GrenadeHandler.EBombType.LongRange: targetPool = longEffectPool; break;
    //        case GrenadeHandler.EBombType.PutRange: targetPool = putEffectPool; break;
    //    }

    //    if (targetPool == null || targetPool.Count == 0) return;

    //    // 未使用のパーティクルを探す
    //    ParticleSystem ps = null;
    //    foreach (var p in targetPool)
    //    {
    //        if (!p.gameObject.activeSelf)
    //        {
    //            ps = p;
    //            break;
    //        }
    //    }

    //    // 全て使用中なら、一番古いもの（リスト先頭）を再利用
    //    if (ps == null)
    //    {
    //        ps = targetPool[0];
    //        targetPool.RemoveAt(0);
    //        targetPool.Add(ps); // 末尾に回す
    //    }

    //    // 再生処理
    //    ps.transform.position = position;
    //    ps.gameObject.SetActive(true);
    //    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    //    ps.Play();
    //}




    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Debug.Log("GrenadePoolManager: Clearing instance");
            Instance = null;
        }

        hasInitialized = false;

        base.Despawned(runner, hasState);
    }



    /// <summary>
    /// プールを初期化（サーバーのみ）
    /// </summary>
    private void InitializeGrenadePool(NetworkObject prefab, int size, Queue<NetworkObject> pool)
    {
        if (prefab == null)
        {
            Debug.LogError("GrenadePoolManager: Prefab is null!");
            return;
        }

        for (int i = 0; i < size; i++)
        {
            // 画面外の遠い位置に配置（レンダリング負荷を避ける）
            Vector3 hiddenPosition = new Vector3(0, -1000f, 0);

            NetworkObject grenade = Runner.Spawn(
                prefab,
                hiddenPosition,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    var handler = obj.GetComponent<GrenadeHandler>();
                    if (handler != null)
                    {
                        handler.SetPoolManager(this);
                    }

                    // 初期状態で完全に無効化
                    InitialDisableGrenade(obj);
                }
            );

            pool.Enqueue(grenade);
        }
    }

    /// <summary>
    /// 初期化時のグレネード無効化（ローカルのみ、RPCなし）
    /// </summary>
    private void InitialDisableGrenade(NetworkObject grenade)
    {
        if (grenade == null || !grenade.IsValid) return;

        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            // 重要: 速度を設定する前にkinematicをfalseにする
            rb.Rigidbody.isKinematic = false;

            // 速度をリセット（kinematicがfalseなので警告なし）
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            // その後でkinematicをtrueにして物理を無効化
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.useGravity = false;
        }

        // Collider を無効化
        var colliders = grenade.GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Renderer を無効化
        var renderers = grenade.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
    }
    /// <summary>
    /// プールからグレネードを取得
    /// ワープ問題を解決: Rigidbody.MovePositionとRPCの組み合わせ
    /// </summary>
    public void GetFromPool(
        Vector3 position,
        Quaternion rotation,
        Vector3 throwForce,
        PlayerRef ownerRef,
        NetworkObject ownerObj,
        string ownerName,
        GrenadeHandler.EBombType type)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("GetFromPool should only be called on server!");
            return;
        }

        Queue<NetworkObject> targetPool = GetPoolByType(type);
        NetworkObject prefab = GetPrefabByType(type);

        NetworkObject grenade;

        if (targetPool.Count > 0)
        {
            grenade = targetPool.Dequeue();
        }
        else
        {
            if (activeGrenades.Count >= maxPoolSize)
            {
                Debug.LogWarning($"GrenadePoolManager: Max pool size ({maxPoolSize}) reached!");
                return;
            }

            Debug.LogWarning($"GrenadePoolManager: Pool empty, spawning new grenade for {type}");
            Vector3 hiddenPosition = new Vector3(0, -1000f, 0);
            grenade = Runner.Spawn(prefab, hiddenPosition, Quaternion.identity);

            var handler = grenade.GetComponent<GrenadeHandler>();
            if (handler != null)
            {
                handler.SetPoolManager(this);
            }
        }

        // 重要: まずGrenadeHandlerをリセット
        GrenadeHandler grenadeHandler = grenade.GetComponent<GrenadeHandler>();
        if (grenadeHandler != null)
        {
            grenadeHandler.ResetState();
        }

        // ワープ対策1: Rigidbodyの位置を物理エンジン経由で設定
        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            // kinematicをfalseにしてから速度リセット
            rb.Rigidbody.isKinematic = false;
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            // その後でkinematicをtrueにして位置設定
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.position = position;
            rb.Rigidbody.rotation = rotation;
        }

        // ワープ対策2: RPCで全クライアントに位置を同期してから有効化
        RPC_ActivateGrenade(grenade, position, rotation, throwForce, ownerRef, ownerObj, ownerName, type);

        activeGrenades.Add(grenade);
    }

    /// <summary>
    /// 改善版RPC: 位置同期とアクティベーションを同時実行
    /// これによりワープ現象を防ぐ
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateGrenade(
        NetworkObject grenade,
        Vector3 position,
        Quaternion rotation,
        Vector3 throwForce,
        PlayerRef ownerRef,
        NetworkObject ownerObj,
        string ownerName,
        GrenadeHandler.EBombType type)
    {
        if (grenade == null || !grenade.IsValid) return;
        grenade.transform.position = position;
        grenade.transform.rotation = rotation;

        // 全クライアントで位置を確実に設定
        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            // kinematicをfalseにしてから速度リセット
            rb.Rigidbody.isKinematic = false;
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            rb.Rigidbody.position = position;
            rb.Rigidbody.rotation = rotation;

            // その後でkinematicをtrueにして位置設定
            rb.Rigidbody.isKinematic = true;
        }

        // Collider を有効化
        var colliders = grenade.GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }

        // Renderer を有効化
        var renderers = grenade.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }

        // GrenadeHandlerのThrowを呼び出す前にもう一度位置確認
        GrenadeHandler grenadeHandler = grenade.GetComponent<GrenadeHandler>();
        if (grenadeHandler != null)
        {
            // Throwメソッド内でkinematicをfalseにして投擲
            grenadeHandler.Throw(throwForce, ownerRef, ownerObj, ownerName, type);
        }
    }

    /// <summary>
    /// グレネードをプールに返却
    /// </summary>
    public void ReturnToPool(NetworkObject grenade, GrenadeHandler.EBombType type)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("ReturnToPool should only be called on server!");
            return;
        }

        if (grenade == null || !grenade.IsValid)
        {
            Debug.LogWarning("Trying to return invalid grenade to pool");
            return;
        }

        activeGrenades.Remove(grenade);

        // RPCで全クライアントに無効化を伝える
        RPC_DisableGrenade(grenade);

        // サーバー側でプールに戻す前に位置をリセット
        Vector3 hiddenPosition = new Vector3(0, -1000f, 0);
        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            // kinematicをfalseにしてから速度リセット
            rb.Rigidbody.isKinematic = false;
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            // その後でkinematicをtrueにして位置リセット
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.useGravity = false;
            rb.Rigidbody.position = hiddenPosition;
            rb.Rigidbody.rotation = Quaternion.identity;
        }

        Queue<NetworkObject> targetPool = GetPoolByType(type);
        targetPool.Enqueue(grenade);
    }

    /// <summary>
    /// RPC: 全クライアントでグレネードを無効化
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DisableGrenade(NetworkObject grenade)
    {
        if (grenade == null || !grenade.IsValid) return;

        // Rigidbody を完全停止
        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            // kinematicをfalseにしてから速度リセット
            rb.Rigidbody.isKinematic = false;
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            // その後でkinematicをtrueにして画面外に移動
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.useGravity = false;
            rb.Rigidbody.position = new Vector3(0, -1000f, 0);
        }

        // Collider を無効化
        var colliders = grenade.GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Renderer を無効化
        var renderers = grenade.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    private Queue<NetworkObject> GetPoolByType(GrenadeHandler.EBombType type)
    {
        switch (type)
        {
            case GrenadeHandler.EBombType.ShortRange:
                return shortRangePool;
            case GrenadeHandler.EBombType.LongRange:
                return longRangePool;
            case GrenadeHandler.EBombType.PutRange:
                return putRangePool;
            default:
                Debug.LogError($"Unknown bomb type: {type}");
                return shortRangePool;
        }
    }

    private NetworkObject GetPrefabByType(GrenadeHandler.EBombType type)
    {
        switch (type)
        {
            case GrenadeHandler.EBombType.ShortRange:
                return shortRangeGrenadePrefab;
            case GrenadeHandler.EBombType.LongRange:
                return longRangeGrenadePrefab;
            case GrenadeHandler.EBombType.PutRange:
                return putRangeGrenadePrefab;
            default:
                Debug.LogError($"Unknown bomb type: {type}");
                return shortRangeGrenadePrefab;
        }
    }

    private void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        int y = 120;
        GUI.Label(new Rect(10, y, 400, 20),
            $"Grenade Pool - Short: {shortRangePool.Count} / Long: {longRangePool.Count} / Put: {putRangePool.Count}");
        GUI.Label(new Rect(10, y + 20, 400, 20),
            $"Active: {activeGrenades.Count} / Max: {maxPoolSize}");
    }
}



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