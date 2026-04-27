using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
// using UnityEditor.Experimental.GraphView;
//using UnityEngine.UI;



public class HPHandler : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnHPChanged))]
    byte HP { get; set; }
    public byte CurrentHP => HP;

    [Networked(OnChanged = nameof(OnStateChanged))]
    public bool isDead { get; set; }

    [Networked]
    public TickTimer InvincibilityTimer { get; set; }

    public static event Action<NetworkPlayer, NetworkPlayer> OnKilled;
    public static event Action<NetworkPlayer> OnDied;

    bool isInitialized = false;


    const byte startingHP = 3;

    //    public Color uiOnHitColor;
    //    public Image uiOnHitImage;

    public SkinnedMeshRenderer bodyMeshRenderer;
    Color defaultMeshBodyColor;

    //    List<FlashMeshRenderer> flashMeshRenderers = new List<FlashMeshRenderer>();

    public GameObject playerModel;
    //public GameObject deathGameObjectPrefab;
    public bool skipSettingStartValues = false;

    //    //Other components
    HitboxRoot hitboxRoot;
    CharacterMovementHandler characterMovementHandler;
    private PlayerAnimationHandler playerAnimationHandler;
    //    NetworkInGameMessages networkInGameMessages;
    //    NetworkPlayer networkPlayer;


    private Renderer[] renderers;
    private bool wasInvincible = false;

    private void Awake()
    {
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
        hitboxRoot = GetComponentInChildren<HitboxRoot>();
        playerAnimationHandler = GetComponent<PlayerAnimationHandler>();
        //networkInGameMessages = GetComponent<NetworkInGameMessages>();
        //networkPlayer = GetComponent<NetworkPlayer>();
    }

    //    // Start is called before the first frame update
    void Start()
    {
        if (!skipSettingStartValues)
        {
            HP = startingHP;
            isDead = false;
        }


        defaultMeshBodyColor = bodyMeshRenderer.material.color;

        isInitialized = true;

        renderers = GetComponentsInChildren<Renderer>();
    }

    public override void Render()
    {
        if (isDead || playerModel == null) return;

        bool isInvincible = !InvincibilityTimer.ExpiredOrNotRunning(Runner);

        if (isInvincible)
        {
            bool isVisible = (Time.time % 0.2f) > 0.1f;
            foreach (var renderer in renderers)
            {
                renderer.enabled = isVisible;
            }
            wasInvincible = true;
        }
        else if (wasInvincible)
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
            wasInvincible = false;
        }
    }
    //    public void ResetMeshRenderers()
    //    {
    //        //Clear old
    //        flashMeshRenderers.Clear();

    //        MeshRenderer[] meshRenderers = playerModel.GetComponentsInChildren<MeshRenderer>();
    //        foreach (MeshRenderer meshRenderer in meshRenderers)
    //            flashMeshRenderers.Add(new FlashMeshRenderer(meshRenderer, null));


    //        SkinnedMeshRenderer[] skinnedMeshRenderers = playerModel.GetComponentsInChildren<SkinnedMeshRenderer>();
    //        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
    //            flashMeshRenderers.Add(new FlashMeshRenderer(null, skinnedMeshRenderer));
    //    }

    IEnumerator OnHitCO()
    {
        bodyMeshRenderer.material.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        bodyMeshRenderer.material.color = defaultMeshBodyColor;

        //foreach (FlashMeshRenderer flashMeshRenderer in flashMeshRenderers)
        //    flashMeshRenderer.ChangeColor(Color.red);

        //if (Object.HasInputAuthority)
        //    uiOnHitImage.color = uiOnHitColor;

        //yield return new WaitForSeconds(0.2f);

        //foreach (FlashMeshRenderer flashMeshRenderer in flashMeshRenderers)
        //    flashMeshRenderer.RestoreColor();

        //if (Object.HasInputAuthority && !isDead)
        //    uiOnHitImage.color = new Color(0, 0, 0, 0);
    }

    IEnumerator ServerReviveCO()
    {
        yield return new WaitForSeconds(2.0f);

        characterMovementHandler.RequestRespawn();
    }


    //    //Function only called on the server & used by weaponHandler
    public void OnTakeDamage(NetworkPlayer damageCausedByPlayerNickname, byte damageAmount)
    {
        if (InvincibilityTimer.ExpiredOrNotRunning(Runner) == false)
        {
            return;
        }
        //if (!Object.HasStateAuthority)
        //    return;

        if (isDead)
            return;

        // Ensure that we cannot flip the byte as it can't handle minuys values
        if (damageAmount > HP)
            damageAmount = HP;
        HP -= damageAmount;


        var victim = GetComponent<NetworkPlayer>();
        if (damageCausedByPlayerNickname != victim)
        {
            if (Object.HasStateAuthority)
                OnKilled?.Invoke(damageCausedByPlayerNickname, victim);
        }

        Debug.Log($"{Time.time} {transform.name} took damage got {HP} left");

        // Player died
        if (HP <= 0)
        {
            Debug.Log($"Time.time) {transform.name} died");

            StartCoroutine(ServerReviveCO());
            if (Object.HasStateAuthority)
                OnDied?.Invoke(GetComponent<NetworkPlayer>());
            isDead = true;
        }
        else
        {
            InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 4.0f);
        }

    }

    static void OnHPChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time} OnHPChanged value {changed.Behaviour.HP}");

        byte newHP = changed.Behaviour.HP;

        //        //Load the old value
        changed.LoadOld();

        byte oldHP = changed.Behaviour.HP;

        //        //Check if the HP has been decreased
        if (newHP < oldHP)
            changed.Behaviour.OnHPReduced();

    }

    private void OnHPReduced()
    {
        if (!isInitialized)
            return;


        Debug.Log("[HP] OnHPReduced called!");
        StartCoroutine(OnHitCO());

        playerAnimationHandler?.TriggerGetDamage(1.0f);
    }

    static void OnStateChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time} OnStateChanged isDead {changed.Behaviour.isDead}");

        bool isDeadCurrent = changed.Behaviour.isDead;

        ////Load the old value
        changed.LoadOld();

        bool isDeadOld = changed.Behaviour.isDead;

        ////Handle on death for the player. Also check if the player was dead but is now alive in that case revive the player.
        if (isDeadCurrent)
            changed.Behaviour.OnDeath();
        else if (!isDeadCurrent && isDeadOld)
            changed.Behaviour.OnRevive();
    }

    private void OnDeath()
    {
        // DeathSFX
        if (Object.HasInputAuthority)
        {
            AudioManager.Play("DeathSFX", AudioManager.MixerTarget.SFX);
        }

        Debug.Log($"{Time.time} OnDeath");

        playerModel.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;
        characterMovementHandler.SetCharacterControllerEnabled(false);

        if (EffectPoolManager.Instance != null)
        {
            EffectPoolManager.Instance.Play(EffectPoolManager.EffectType.Death, transform.position);
            EffectPoolManager.Instance.Play(EffectPoolManager.EffectType.Death2, transform.position);
        }
    }

    private void OnRevive()
    {
        Debug.Log($"{Time.time} OnRevive");

        //if (Object.HasInputAuthority)
        //    uiOnHitImage.color = new Color(0, 0, 0, 0);

        playerModel.gameObject.SetActive(true);
        hitboxRoot.HitboxRootActive = true;
        characterMovementHandler.SetCharacterControllerEnabled(true);
    }

    public void OnRespawned()
    {
        //Reset variables
        HP = startingHP;
        isDead = false;

        InvincibilityTimer = TickTimer.None;

    }




}