using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
//using UnityEngine.UI;

public class HPHandler : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnHPChanged))]
    byte HP { get; set; }

    [Networked(OnChanged = nameof(OnStateChanged))]
    public bool isDead { get; set; }

    bool isInitialized = false; //このスクリプトが初期化されているかを確認


    const byte startingHP = 3;

    //    public Color uiOnHitColor;
    //    public Image uiOnHitImage;

    public MeshRenderer bodyMeshRenderer;
    Color defaultMeshBodyColor;

    //    List<FlashMeshRenderer> flashMeshRenderers = new List<FlashMeshRenderer>();

    public GameObject playerModel;
    public GameObject deathGameObjectPrefab;

    //    public bool skipSettingStartValues = false;

    //    //Other components
    HitboxRoot hitboxRoot;
    CharacterMovementHandler characterMovementHandler;
    //    NetworkInGameMessages networkInGameMessages;
    //    NetworkPlayer networkPlayer;

    private void Awake()
    {
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
        hitboxRoot = GetComponentInChildren<HitboxRoot>();
        //networkInGameMessages = GetComponent<NetworkInGameMessages>();
        //networkPlayer = GetComponent<NetworkPlayer>();
    }

    //    // Start is called before the first frame update
    void Start()
    {
        HP = startingHP;
        isDead = false;

        defaultMeshBodyColor = bodyMeshRenderer.material.color;

        isInitialized = true;
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

    IEnumerator ServerReviveCO() //リスポーンリクエストを出す
    {
        yield return new WaitForSeconds(2.0f);

        Debug.Log($"リクエスリスポーン for {transform.name}");
        characterMovementHandler.RequestRespawn();
    }


    //    //Function only called on the server & used by weaponHandler
    public void OnTakeDamage(string damageCausedByPlayerNickname, byte damageAmount)
    {
        ////Only take damage while alive 死んでたらそれ以上ダメージ受けなくてよい
        if (isDead)
            return;

        // Ensure that we cannot flip the byte as it can't handle minuys values
        if (damageAmount > HP)
            damageAmount = HP;


        HP -= damageAmount;
        Debug.Log($"{Time.time} {transform.name} took damage got {HP} left");

        // Player died
        if (HP <= 0)
        {
            Debug.Log($"Time.time) {transform.name} died");

            StartCoroutine(ServerReviveCO());

            isDead = true;
        }
    }

    static void OnHPChanged(Changed<HPHandler> changed) // HPがへっている場合のみを扱う
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

        StartCoroutine(OnHitCO());
    }

    static void OnStateChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time} OnStateChanged isDead {changed.Behaviour.isDead}");

        bool isDeadCurrent = changed.Behaviour.isDead; // HPと同様の方法で実装

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
        Debug.Log($"{Time.time} OnDeath");

        playerModel.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false; // 死んだプレイヤーに続けて攻撃できないようにする
        characterMovementHandler.SetCharacterControllerEnabled(false);

        Instantiate(deathGameObjectPrefab, transform.position, Quaternion.identity);
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
    }
}
