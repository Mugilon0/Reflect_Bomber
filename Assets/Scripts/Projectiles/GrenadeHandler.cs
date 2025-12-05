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

    // プールマネージャーへの参照
    private GrenadePoolManager poolManager;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();
    }

    /// <summary>
    /// プールマネージャーを設定（プール初期化時に呼ばれる）
    /// </summary>
    public void SetPoolManager(GrenadePoolManager manager)
    {
        poolManager = manager;
    }

    /// <summary>
    /// 状態をリセット（プールから取得時に呼ばれる）
    /// </summary>
    public void ResetState()
    {
        hasExploded = false;
        IsCollisionExplosionQueued = false;
        explodeTickTimer = TickTimer.None;
        thrownByPlayerRef = PlayerRef.None;
        thrownByPlayerName = string.Empty;
        thrownByNetworkObject = null;

        // Rigidbodyを完全リセット（警告を出さない順序）
        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
        {
            // 1. まずkinematicをtrueに
            networkRigidbody.Rigidbody.isKinematic = true;
            networkRigidbody.Rigidbody.useGravity = false;

            // 2. kinematicがtrueの状態で速度をリセット（警告なし）
            networkRigidbody.Rigidbody.velocity = Vector3.zero;
            networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Kinematic警告を解決した改善版Throw
    /// 重要: kinematicをfalseにしてから速度を設定する
    /// </summary>
    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, NetworkObject thrownByNetworkObject, string thrownByPlayerName, EBombType type)
    {
        bombType = type;

        switch (bombType)
        {
            case EBombType.ShortRange:
                explosionRadius = 1.0f;
                break;
            case EBombType.LongRange:
                explosionRadius = 4.0f;
                break;
            case EBombType.PutRange:
                explosionRadius = 3.0f;
                break;
        }

        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;
        this.thrownByNetworkObject = thrownByNetworkObject;

        PlayThrowSound(transform.position, bombType);


        // 警告対策: 正しい順序でkinematicと速度を設定
        if (networkRigidbody != null && networkRigidbody.Rigidbody != null)
        {
            // 置きボムかどうかで処理を分岐
            if (bombType == EBombType.PutRange)
            {
                // 置きボム: kinematicのまま、位置を固定
                networkRigidbody.Rigidbody.isKinematic = false;
                networkRigidbody.Rigidbody.useGravity = false;
                networkRigidbody.Rigidbody.velocity = Vector3.zero;
                networkRigidbody.Rigidbody.angularVelocity = Vector3.zero;

                networkRigidbody.Rigidbody.constraints = RigidbodyConstraints.FreezeAll;

                // デバッグログ（必要に応じて）
                // Debug.Log($"[PutBomb] Position set to: {transform.position}");
            }
            else
            {
                // 投擲ボム: 通常の物理演算
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
        // 無効状態ではFixedUpdateを実行しない
        if (networkRigidbody == null || networkRigidbody.Rigidbody == null) return;
        //if (networkRigidbody.Rigidbody.isKinematic) return; // kinematic状態ではスキップ

        if (Object.HasStateAuthority)
        {
            // カスタム重力を適用
            networkRigidbody.Rigidbody.AddForce(new Vector3(0, customGravity, 0), ForceMode.Acceleration);
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

        int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpackPoint.position, hitCountRange, thrownByPlayerRef, hits, collisionLayers, HitOptions.IncludePhysX);

        if (hitCount > 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (!Object.HasStateAuthority || !Object.IsValid) return;

        // ...(ダメージ判定等のロジックは既存のまま)...
        int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, explosionRadius, PlayerRef.None, hits, ~0, HitOptions.None);

        for (int i = 0; i < hitCount; i++)
        {
            // ... (省略: ダメージや誘爆の処理) ...
            if (hits[i].Hitbox != null)
            {
                HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();
                if (hpHandler != null && thrownByNetworkObject != null && thrownByNetworkObject.IsValid)
                {
                    NetworkPlayer attackerPlayer = thrownByNetworkObject.GetComponent<NetworkPlayer>();
                    if (attackerPlayer != null) hpHandler.OnTakeDamage(attackerPlayer, 1);
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

        // ★修正: RPCに「座標」と「ボムの種類」を渡す
        RPC_PlayExplosionAndSound(transform.position, bombType);

        // プールへ返却
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
        // ★変更: GrenadePoolManager ではなく EffectPoolManager を使う
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






    /// <summary>
    /// 爆発エフェクトと音を再生
    /// </summary>
    private void PlayExplosionEffects()
    {
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>();
        if (grenadeMesh == null) return;

        Vector3 explosionPosition = grenadeMesh.transform.position;

        // エフェクト再生
        if (explosionParticleSystemPrefab != null)
        {
            Instantiate(explosionParticleSystemPrefab, explosionPosition, Quaternion.identity);
        }

        // 音再生
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
        // プールを使用している場合はここは通らない
        if (poolManager != null) return;

        // プールを使っていない場合のみエフェクトを再生
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