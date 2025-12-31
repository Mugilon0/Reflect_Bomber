using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Fusion1対応のグレネードプール（ワープ残像修正・コンパイルエラー対応版）
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

    // Queueを使用してFIFO方式で管理
    private Queue<NetworkObject> shortRangePool = new Queue<NetworkObject>();
    private Queue<NetworkObject> longRangePool = new Queue<NetworkObject>();
    private Queue<NetworkObject> putRangePool = new Queue<NetworkObject>();

    private HashSet<NetworkObject> activeGrenades = new HashSet<NetworkObject>();

    public static GrenadePoolManager Instance { get; private set; }

    private bool hasInitialized = false;

    // 非表示待機場所
    private readonly Vector3 hiddenPosition = new Vector3(0, -1000f, 0);

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate GrenadePoolManager detected. Keeping original. HasStateAuthority: {Object.HasStateAuthority}");

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
            return;
        }

        // サーバーだけがプールを初期化
        if (Object.HasStateAuthority)
        {
            InitializeGrenadePool(shortRangeGrenadePrefab, shortRangePoolSize, shortRangePool);
            InitializeGrenadePool(longRangeGrenadePrefab, longRangePoolSize, longRangePool);
            InitializeGrenadePool(putRangeGrenadePrefab, putRangePoolSize, putRangePool);
        }

        hasInitialized = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
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
        if (prefab == null) return;

        for (int i = 0; i < size; i++)
        {
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

    private void InitialDisableGrenade(NetworkObject grenade)
    {
        if (grenade == null || !grenade.IsValid) return;

        TeleportGrenade(grenade, hiddenPosition, Quaternion.identity);

        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.useGravity = false;
        }

        SetGrenadeVisuals(grenade, false);
    }

    /// <summary>
    /// プールからグレネードを取得
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
            if (activeGrenades.Count >= maxPoolSize) return;

            grenade = Runner.Spawn(prefab, hiddenPosition, Quaternion.identity);
            var handler = grenade.GetComponent<GrenadeHandler>();
            if (handler != null) handler.SetPoolManager(this);
        }

        // 状態リセット
        GrenadeHandler grenadeHandler = grenade.GetComponent<GrenadeHandler>();
        if (grenadeHandler != null) grenadeHandler.ResetState();

        // 【重要】サーバー側で即座にワープ（補間リセット）
        TeleportGrenade(grenade, position, rotation);

        // RPCで全クライアントへ同期
        RPC_ActivateGrenade(grenade, position, rotation, throwForce, ownerRef, ownerObj, ownerName, type);

        activeGrenades.Add(grenade);
    }

    /// <summary>
    /// 全クライアントで有効化＆ワープ同期
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

        // 【重要】クライアント側でもTeleportを実行して補間（残像）を消す
        TeleportGrenade(grenade, position, rotation);

        SetGrenadeVisuals(grenade, true);

        GrenadeHandler grenadeHandler = grenade.GetComponent<GrenadeHandler>();
        if (grenadeHandler != null)
        {
            grenadeHandler.Throw(throwForce, ownerRef, ownerObj, ownerName, type);
        }
    }

    public void ReturnToPool(NetworkObject grenade, GrenadeHandler.EBombType type)
    {
        if (!Object.HasStateAuthority) return;

        if (grenade == null || !grenade.IsValid) return;

        activeGrenades.Remove(grenade);

        RPC_DisableGrenade(grenade);

        // 地下へワープ
        TeleportGrenade(grenade, hiddenPosition, Quaternion.identity);

        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            rb.Rigidbody.isKinematic = true;
            rb.Rigidbody.useGravity = false;
        }

        Queue<NetworkObject> targetPool = GetPoolByType(type);
        targetPool.Enqueue(grenade);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DisableGrenade(NetworkObject grenade)
    {
        if (grenade == null || !grenade.IsValid) return;

        TeleportGrenade(grenade, hiddenPosition, Quaternion.identity);
        SetGrenadeVisuals(grenade, false);
    }

    // ==================================================================================
    // 修正されたワープ用ヘルパーメソッド
    // ==================================================================================

    /// <summary>
    /// 安全にオブジェクトをワープさせるメソッド
    /// NetworkTransformのTeleportTo系を使用し、エラーを回避しつつ補間をリセットする
    /// </summary>
    private void TeleportGrenade(NetworkObject grenade, Vector3 position, Quaternion rotation)
    {
        if (grenade == null) return;

        bool teleported = false;

        // 1. NetworkTransform がある場合 (Fusion 1推奨)
        // Teleportメソッドがないバージョンのために TeleportToPosition / TeleportToRotation を使用
        var nt = grenade.GetComponent<NetworkTransform>();
        if (nt != null)
        {
            nt.TeleportToPosition(position);
            nt.TeleportToRotation(rotation);
            teleported = true;
        }

        // 2. 物理演算のリセット
        var rb = grenade.GetComponent<NetworkRigidbody>();
        if (rb != null && rb.Rigidbody != null)
        {
            rb.Rigidbody.velocity = Vector3.zero;
            rb.Rigidbody.angularVelocity = Vector3.zero;

            // NetworkTransformがなく、NetworkRigidbodyのみで同期している場合のフォールバック
            if (!teleported)
            {
                rb.Rigidbody.position = position;
                rb.Rigidbody.rotation = rotation;

                // 補間ターゲット（描画用）を強制的に移動させて残像を消す
                if (rb.InterpolationTarget != null)
                {
                    rb.InterpolationTarget.position = position;
                    rb.InterpolationTarget.rotation = rotation;
                }
                teleported = true;
            }
        }

        // 3. どちらもない場合（Transform同期のみ）
        if (!teleported)
        {
            grenade.transform.position = position;
            grenade.transform.rotation = rotation;
        }
    }

    private void SetGrenadeVisuals(NetworkObject grenade, bool isActive)
    {
        var colliders = grenade.GetComponents<Collider>();
        foreach (var col in colliders) col.enabled = isActive;

        var renderers = grenade.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers) renderer.enabled = isActive;
    }

    private Queue<NetworkObject> GetPoolByType(GrenadeHandler.EBombType type)
    {
        switch (type)
        {
            case GrenadeHandler.EBombType.ShortRange: return shortRangePool;
            case GrenadeHandler.EBombType.LongRange: return longRangePool;
            case GrenadeHandler.EBombType.PutRange: return putRangePool;
            default: return shortRangePool;
        }
    }

    private NetworkObject GetPrefabByType(GrenadeHandler.EBombType type)
    {
        switch (type)
        {
            case GrenadeHandler.EBombType.ShortRange: return shortRangeGrenadePrefab;
            case GrenadeHandler.EBombType.LongRange: return longRangeGrenadePrefab;
            case GrenadeHandler.EBombType.PutRange: return putRangeGrenadePrefab;
            default: return shortRangeGrenadePrefab;
        }
    }

    private void OnGUI()
    {
        if (!Debug.isDebugBuild) return;
        int y = 120;
        GUI.Label(new Rect(10, y, 400, 20), $"Short: {shortRangePool.Count} / Long: {longRangePool.Count} / Put: {putRangePool.Count}");
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