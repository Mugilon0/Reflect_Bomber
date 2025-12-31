using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GrenadeHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public Transform checkForImpackPoint;
    public LayerMask collisionLayers;

    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    // Thrown by info
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;
    NetworkObject thrownByNetworkObject;

    // Timing
    TickTimer explodeTickTimer = TickTimer.None;

    // other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    [Networked]
    private NetworkBool IsCollisionExplosionQueued { get; set; }

    private bool hasExploded = false;

    [SerializeField]
    private float hitCountRange = 0.5f;

    // Lag補償用
    [Networked]
    private int SpawnTick { get; set; }

    [SerializeField]
    private int lagCompDisableTicks = 4;

    public enum EBombType
    {
        ShortRange,
        LongRange,
        PutRange
    }
    [Networked] private EBombType bombType { get; set; }

    private float explosionRadius;

    [SerializeField]
    private float customGravity = -9.8f;

    private GrenadePoolManager poolManager;

    // ========== 自爆防止（投げボムのみ） ==========

    [Header("Self Damage Protection (Thrown Bombs Only)")]
    [SerializeField]
    [Tooltip("この距離を移動するまで投げた本人へのダメージを無効（ShortRange/LongRangeのみ）")]
    private float selfDamageProtectionDistance = 2.0f;

    [Networked]
    private Vector3 SpawnPosition { get; set; }

    [Networked]
    private NetworkBool IsSelfDamageEnabled { get; set; }

    // キャッシュ用（平方根計算を避ける）
    private float sqrProtectionDistance;

    // ================================================

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        // 平方距離を事前計算
        sqrProtectionDistance = selfDamageProtectionDistance * selfDamageProtectionDistance;
    }

    public void SetPoolManager(GrenadePoolManager manager)
    {
        poolManager = manager;
    }

    public void ResetState()
    {
        hasExploded = false;
        IsCollisionExplosionQueued = false;
        explodeTickTimer = TickTimer.None;
        thrownByPlayerRef = PlayerRef.None;
        thrownByPlayerName = string.Empty;
        thrownByNetworkObject = null;

        // 自爆保護リセット
        IsSelfDamageEnabled = false;
        SpawnPosition = Vector3.zero;

        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
        {
            networkRigidbody.Rigidbody.isKinematic = true;
            networkRigidbody.Rigidbody.useGravity = false;
            networkRigidbody.Rigidbody.velocity = Vector3.zero;
            networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, NetworkObject thrownByNetworkObject, string thrownByPlayerName, EBombType type)
    {
        bombType = type;
        SpawnTick = Runner.Simulation.Tick;

        switch (bombType)
        {
            case EBombType.ShortRange:
                explosionRadius = 1.0f;
                // 投げボム: 自爆保護を有効化
                SpawnPosition = transform.position;
                IsSelfDamageEnabled = false;
                break;

            case EBombType.LongRange:
                explosionRadius = 4.0f;
                // 投げボム: 自爆保護を有効化
                SpawnPosition = transform.position;
                IsSelfDamageEnabled = false;
                break;

            case EBombType.PutRange:
                explosionRadius = 3.0f;
                // 置きボム: 自爆保護なし（従来通り）
                IsSelfDamageEnabled = true;
                break;
        }

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;
        this.thrownByNetworkObject = thrownByNetworkObject;

        PlayThrowSound(transform.position, bombType);

        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
        {
            if (bombType == EBombType.PutRange)
            {
                networkRigidbody.Rigidbody.isKinematic = false;
                networkRigidbody.Rigidbody.useGravity = false;
                networkRigidbody.Rigidbody.velocity = Vector3.zero;
                networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
                networkRigidbody.Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                networkRigidbody.Rigidbody.isKinematic = false;
                networkRigidbody.Rigidbody.useGravity = true;
                networkRigidbody.Rigidbody.velocity = Vector3.zero;
                networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
                networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

                Vector3 frontFlip = transform.right * 10f;
                networkRigidbody.Rigidbody.angularVelocity = frontFlip;
            }
        }

        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 4);
    }

    public override void FixedUpdateNetwork()
    {
        if (networkRigidbody == null || networkRigidbody.Rigidbody == null) return;

        if (Object.HasStateAuthority)
        {
            networkRigidbody.Rigidbody.AddForce(new Vector3(0, customGravity, 0), ForceMode.Acceleration);

            // ========== 移動距離チェック（投げボムのみ、保護中のみ） ==========
            if (!IsSelfDamageEnabled && bombType != EBombType.PutRange)
            {
                // sqrMagnitudeで平方根を回避
                float sqrDistance = (transform.position - SpawnPosition).sqrMagnitude;
                if (sqrDistance >= sqrProtectionDistance)
                {
                    IsSelfDamageEnabled = true;
                }
            }
            // ================================================================
        }

        if (!Object.IsValid) return;

        if (explodeTickTimer.Expired(Runner))
        {
            explodeTickTimer = TickTimer.None;
            Explode();
            return;
        }

        if (IsCollisionExplosionQueued)
        {
            Explode();
            return;
        }

        if (Runner.Simulation.Tick >= SpawnTick + lagCompDisableTicks + lagCompDisableTicks)
        {
            int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, hitCountRange, thrownByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX);

            if (hitCount > 0)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (!Object.HasStateAuthority || !Object.IsValid) return;

        int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, explosionRadius, PlayerRef.None, hits, ~0, HitOptions.None);

        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].Hitbox != null)
            {
                HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();
                if (hpHandler != null && thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
                {
                    // ========== 自爆チェック ==========
                    // 投げボムで保護中の場合のみスキップ
                    if (!IsSelfDamageEnabled)
                    {
                        NetworkObject hitNetworkObject = hits[i].Hitbox.transform.root.GetComponent<NetworkObject>();
                        if (hitNetworkObject != null && hitNetworkObject == thrownByNetworkObject)
                        {
                            // 自分自身 & 保護中 → スキップ
                            continue;
                        }
                    }
                    // ==================================

                    NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                    if (attackerPlayer != null)
                    {
                        hpHandler.OnTakeDamage(attackerPlayer, 1);
                    }
                }
            }
            if (hits[i].Collider != null)
            {
                GrenadeHandler otherGrenade = hits[i].Collider.GetComponentInParent<GrenadeHandler>();
                if (otherGrenade != null && otherGrenade != this)
                {
                    otherGrenade.Explode();
                }
            }
        }

        RPC_PlayExplosionAndSound(transform.position, bombType);

        if (poolManager != null)
        {
            poolManager.ReturnToPool(Object, bombType);
        }
        else
        {
            Runner.Despawn(Object);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionAndSound(Vector3 position, EBombType type)
    {
        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.EffectType effectType = EffectPoolManager.EffectType.Explosion_Short;

            switch (type)
            {
                case EBombType.ShortRange: effectType = EffectPoolManager.EffectType.Explosion_Short; break;
                case EBombType.LongRange: effectType = EffectPoolManager.EffectType.Explosion_Long; break;
                case EBombType.PutRange: effectType = EffectPoolManager.EffectType.Explosion_Put; break;
            }

            EffectPoolManager.Instance.Play(effectType, position);
        }

        PlayExplosionSound(position, type);
    }

    private void PlayThrowSound(Vector3 position, EBombType type)
    {
        switch (type)
        {
            case EBombType.ShortRange:
                AudioManager.Play("StrawberryThrowSFX", AudioManager.MixerTarget.SFX, position);
                break;
            case EBombType.LongRange:
                AudioManager.Play("OrangeThrowSFX", AudioManager.MixerTarget.SFX, position);
                break;
            case EBombType.PutRange:
                AudioManager.Play("PinappleThrowSFX", AudioManager.MixerTarget.SFX, position);
                break;
        }
    }

    private void PlayExplosionSound(Vector3 position, EBombType type)
    {
        switch (type)
        {
            case EBombType.ShortRange:
                AudioManager.Play("StrawberryExplosionSFX", AudioManager.MixerTarget.SFX, position);
                break;
            case EBombType.LongRange:
                AudioManager.Play("OrangeExplosionSFX", AudioManager.MixerTarget.SFX, position);
                break;
            case EBombType.PutRange:
                AudioManager.Play("PinappleExplosionSFX", AudioManager.MixerTarget.SFX, position);
                break;
        }
    }

    private void PlayExplosionEffectsLegacy(Vector3 position)
    {
        if (explosionParticleSystemPrefab != null)
            Instantiate(explosionParticleSystemPrefab, position, Quaternion.identity);
    }

    private void PlayExplosionEffects()
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>();
        if (grenadeMesh == null) return;

        Vector3 explosionPosition = grenadeMesh.transform.position;

        if (explosionParticleSystemPrefab != null)
        {
            Instantiate(explosionParticleSystemPrefab, explosionPosition, Quaternion.identity);
        }

        switch (bombType)
        {
            case EBombType.ShortRange:
                AudioManager.Play("StrawberryExplosionSFX", AudioManager.MixerTarget.SFX, explosionPosition);
                break;
            case EBombType.LongRange:
                AudioManager.Play("OrangeExplosionSFX", AudioManager.MixerTarget.SFX, explosionPosition);
                break;
            case EBombType.PutRange:
                AudioManager.Play("PinappleExplosionSFX", AudioManager.MixerTarget.SFX, explosionPosition);
                break;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (poolManager != null) return;
        PlayExplosionEffects();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Object == null) return;
        if (hasExploded) return;

        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (bombType == EBombType.LongRange)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                IsCollisionExplosionQueued = true;
                return;
            }
        }

        if (collision.gameObject.TryGetComponent<GrenadeHandler>(out var otherGrenade))
        {
            IsCollisionExplosionQueued = true;
        }
    }
}




















//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Fusion;

//public class GrenadeHandler : NetworkBehaviour
//{
//    [Header("Visuals")]
//    // ★Prefab変更なしでテストしたい場合はコメントアウトのままでOK
//    // [SerializeField] private Transform visualMesh;
//    // [SerializeField] private float visualRotationSpeed = 400f; 

//    [Header("Physics Settings")]
//    // ★重要: 重力倍率。数値を上げると「ズドン」と早く落ち、軌道が鋭くなります。
//    // 2.0f ~ 3.0f くらいがキビキビした動作になります。
//    [SerializeField] private float gravityScale = 11f;

//    [Header("Prefabs")]
//    public GameObject explosionParticleSystemPrefab;

//    [Header("Collision detection")]
//    public Transform checkForImpackPoint;
//    public LayerMask collisionLayers;

//    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

//    PlayerRef thrownByPlayerRef;
//    string thrownByPlayerName;
//    NetworkObject thrownByNetworkObject;

//    TickTimer explodeTickTimer = TickTimer.None;

//    NetworkObject networkObject;
//    NetworkRigidbody networkRigidbody;

//    private GrenadePoolManager poolManager;

//    [Networked]
//    private NetworkBool IsCollisionExplosionQueued { get; set; }

//    private bool hasExploded = false;

//    [SerializeField]
//    private float hitCountRange = 0.5f;

//    [Networked]
//    private int SpawnTick { get; set; }

//    [SerializeField]
//    private int lagCompDisableTicks = 4;

//    public enum EBombType
//    {
//        ShortRange,
//        LongRange,
//        PutRange
//    }

//    [Networked]
//    private EBombType bombType { get; set; }

//    private float explosionRadius;

//    [SerializeField]
//    private byte damage = 10;

//    public override void Spawned()
//    {
//        networkObject = GetComponent<NetworkObject>();
//        networkRigidbody = GetComponent<NetworkRigidbody>();
//        SpawnTick = Runner.Tick;

//        hasExploded = false;
//        IsCollisionExplosionQueued = false;
//    }

//    public void SetPoolManager(GrenadePoolManager manager)
//    {
//        this.poolManager = manager;
//    }

//    public void ResetState()
//    {
//        hasExploded = false;
//        IsCollisionExplosionQueued = false;
//        explodeTickTimer = TickTimer.None;

//        if (networkRigidbody == null) networkRigidbody = GetComponent<NetworkRigidbody>();
//        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
//        {
//            networkRigidbody.Rigidbody.velocity = Vector3.zero;
//            networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
//        }
//    }

//    public override void FixedUpdateNetwork()
//    {
//        // 権限を持っているクライアントのみが処理を行う
//        if (Object.HasStateAuthority)
//        {
//            if (hasExploded) return;

//            // ★追加: カスタム重力処理
//            // PutRange（設置型）以外は重力をかける
//            if (networkRigidbody != null && networkRigidbody.Rigidbody != null && bombType != EBombType.PutRange)
//            {
//                // Physics.gravity (通常 -9.81) に倍率を掛けた力を加える
//                // ForceMode.Acceleration を使うことで質量に関係なく一定の重力を再現
//                networkRigidbody.Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
//            }

//            // 衝突による爆発フラグが立っていたら爆発処理
//            if (IsCollisionExplosionQueued)
//            {
//                Explode();
//                return;
//            }

//            // タイマー経過による爆発
//            if (explodeTickTimer.Expired(Runner))
//            {
//                Explode();
//                return;
//            }
//        }
//    }

//    public override void Render()
//    {
//        // Prefab変更なしでテストする場合はコメントアウト
//        /*
//        if (Object != null && Object.IsValid && !hasExploded && visualMesh != null)
//        {
//            Vector3 rotationAxis = (Vector3.right + Vector3.up).normalized;
//            visualMesh.Rotate(rotationAxis * visualRotationSpeed * Time.deltaTime);
//        }
//        */
//    }

//    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, NetworkObject thrownByNetworkObject, string thrownByPlayerName, EBombType type)
//    {
//        this.thrownByPlayerRef = thrownByPlayerRef;
//        this.thrownByNetworkObject = thrownByNetworkObject;
//        this.thrownByPlayerName = thrownByPlayerName;
//        this.bombType = type;

//        networkObject = GetComponent<NetworkObject>();
//        networkRigidbody = GetComponent<NetworkRigidbody>();

//        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
//        {
//            if (bombType == EBombType.PutRange)
//            {
//                networkRigidbody.Rigidbody.isKinematic = true;
//                networkRigidbody.Rigidbody.useGravity = false;
//                explosionRadius = 3.0f;
//            }
//            else
//            {
//                networkRigidbody.Rigidbody.isKinematic = false;

//                // ★重要: Unity標準の重力はOFFにする（手動でgravityScale倍の重力をかけるため）
//                networkRigidbody.Rigidbody.useGravity = false;

//                // ★重要: 物理的な回転をロック（反射方向を安定させるため）
//                networkRigidbody.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

//                networkRigidbody.Rigidbody.velocity = Vector3.zero;
//                networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;

//                networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);
//            }
//        }

//        if (bombType == EBombType.ShortRange)
//        {
//            explosionRadius = 1.0f;
//        }
//        else if (bombType == EBombType.LongRange)
//        {
//            explosionRadius = 4.0f;
//        }

//        PlayThrowSound(transform.position, bombType);

//        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 4);
//    }

//    public void Explode()
//    {
//        if (hasExploded) return;
//        hasExploded = true;

//        if (!Object.HasStateAuthority || !Object.IsValid) return;

//        if (Runner.LagCompensation.OverlapSphere(transform.position, explosionRadius, PlayerRef.None, hits, ~0, HitOptions.None) > 0)
//        {
//            foreach (var hit in hits)
//            {
//                if (hit.Hitbox != null)
//                {
//                    var root = hit.Hitbox.transform.root;
//                    HPHandler hpHandler = root.GetComponent<HPHandler>();

//                    if (hpHandler != null && thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
//                    {
//                        NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();

//                        if (attackerPlayer != null)
//                        {
//                            hpHandler.OnTakeDamage(attackerPlayer, damage);
//                        }
//                    }
//                }

//                if (hit.Collider != null)
//                {
//                    GrenadeHandler otherGrenade = hit.Collider.GetComponentInParent<GrenadeHandler>();
//                    if (otherGrenade != null && otherGrenade != this)
//                    {
//                        otherGrenade.Explode();
//                    }
//                }
//            }
//        }

//        RPC_PlayExplosionAndSound(transform.position, bombType);

//        if (poolManager != null)
//        {
//            poolManager.ReturnToPool(Object, bombType);
//        }
//        else
//        {
//            Runner.Despawn(Object);
//        }
//    }

//    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
//    private void RPC_PlayExplosionAndSound(Vector3 position, EBombType type)
//    {
//        PlayExplosionSound(position, type);
//        PlayExplosionEffects(position);
//    }

//    private void PlayThrowSound(Vector3 position, EBombType type)
//    {
//        switch (type)
//        {
//            case EBombType.ShortRange:
//                AudioManager.Play("StrawberryThrowSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//            case EBombType.LongRange:
//                AudioManager.Play("OrangeThrowSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//            case EBombType.PutRange:
//                AudioManager.Play("PinappleThrowSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//        }
//    }

//    private void PlayExplosionSound(Vector3 position, EBombType type)
//    {
//        switch (type)
//        {
//            case EBombType.ShortRange:
//                AudioManager.Play("StrawberryExplosionSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//            case EBombType.LongRange:
//                AudioManager.Play("OrangeExplosionSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//            case EBombType.PutRange:
//                AudioManager.Play("PinappleExplosionSFX", AudioManager.MixerTarget.SFX, position);
//                break;
//        }
//    }

//    public override void Despawned(NetworkRunner runner, bool hasState)
//    {
//        if (poolManager == null)
//        {
//            PlayExplosionEffects(transform.position);
//        }
//    }

//    private void PlayExplosionEffects(Vector3 position)
//    {
//        if (explosionParticleSystemPrefab != null)
//        {
//            Instantiate(explosionParticleSystemPrefab, position, Quaternion.identity);
//        }
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        if (Object == null || !Object.IsValid) return;
//        if (hasExploded) return;

//        if (!Object.HasStateAuthority) return;

//        if (bombType == EBombType.LongRange)
//        {
//            if (collision.gameObject.CompareTag("Ground"))
//            {
//                IsCollisionExplosionQueued = true;
//                return;
//            }
//        }

//        if (collision.gameObject.TryGetComponent<GrenadeHandler>(out var otherGrenade))
//        {
//            IsCollisionExplosionQueued = true;
//        }
//    }
//}
