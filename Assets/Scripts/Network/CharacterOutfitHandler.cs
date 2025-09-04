using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterOutfitHandler : NetworkBehaviour
{
    [Header("Ready UI")]
    public Image readyCheckboxImage;


    [Header("Character Visuals")]
    public SkinnedMeshRenderer bodySkinnedMeshRenderer; // BodyのSkinnedMeshRendererをアサイン
    public List<Material> bodyMaterials; // 4つのマテリアルをアサインするリスト


    [Networked(OnChanged = nameof(OnIsDoneWithCharacterSelectionChanged))]
    public NetworkBool isDoneWithCharacterSelection { get; set; }


    [Networked(OnChanged = nameof(OnMaterialIndexChanged))]
    public int MaterialIndex { get; set; } = -1; // -1で初期化

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void Spawned()
    {
        // オブジェクトがクライアント上で生成された際に、既にインデックスが設定されていればマテリアルを適用
        if (MaterialIndex != -1)
        {
            ApplyMaterial();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetInitialMaterialIndex(int index)
    {
        // サーバーのみがこの値を設定できる
        if (Object.HasStateAuthority)
        {
            MaterialIndex = index;

            ApplyMaterial();
        }
    }

    private void ApplyMaterial()
    {
        if (bodySkinnedMeshRenderer != null && bodyMaterials != null && bodyMaterials.Count > 0)
        {
            if (MaterialIndex >= 0 && MaterialIndex < bodyMaterials.Count)
            {
                // インデックスに対応するマテリアルを適用
                bodySkinnedMeshRenderer.material = bodyMaterials[MaterialIndex];
            }
            else
            {
                Debug.LogWarning($"MaterialIndex {MaterialIndex} is out of bounds for bodyMaterials list (count: {bodyMaterials.Count})");
            }
        }
        else
        {
            Debug.LogWarning("SkinnedMeshRenderer or BodyMaterials list is not assigned in CharacterOutfitHandler.");
        }
    }



    public void OnReady(bool isReady)
    {
        //Request host to change the outfit, if we have input authority over the object.
        if (Object.HasInputAuthority)
        {
            RPC_SetReady(isReady);
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetReady(NetworkBool isReady, RpcInfo info = default)
    {
        Debug.Log($"SERVER: RPC_SetReady received from Player {info.Source}. Setting isDoneWithCharacterSelection to {isReady} for NetworkObject {Object.Id}");
        isDoneWithCharacterSelection = isReady;
    }


    static void OnIsDoneWithCharacterSelectionChanged(Changed<CharacterOutfitHandler> changed)
    {
        changed.Behaviour.IsDoneWithCharacterSelectionChanged();
    }

    private void IsDoneWithCharacterSelectionChanged()
    {
        Debug.Log($"{(Runner.IsServer ? "SERVER" : "CLIENT " + Runner.LocalPlayer)}: IsDoneWithCharacterSelectionChanged. Value: {isDoneWithCharacterSelection}, Scene: {SceneManager.GetActiveScene().name}, readyCheckboxImage active: {isDoneWithCharacterSelection && SceneManager.GetActiveScene().name == "Ready"}");
        // キャラ変更して際トリガーされないための保険(?)
        if (SceneManager.GetActiveScene().name != "Ready")
            return;

        if (isDoneWithCharacterSelection)
            readyCheckboxImage.gameObject.SetActive(true);
        else readyCheckboxImage.gameObject.SetActive(false);
    }



    static void OnMaterialIndexChanged(Changed<CharacterOutfitHandler> changed)
    {
        changed.Behaviour.ApplyMaterial();
    }

    void OnEnable()
    {
        Debug.Log($"NetworkPlayer ({Object?.Id}): OnEnable called. Subscribing to sceneLoaded. Current scene: {SceneManager.GetActiveScene().name}");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        Debug.Log($"NetworkPlayer ({Object?.Id}): OnDisable called. Unsubscribing from sceneLoaded. Current scene: {SceneManager.GetActiveScene().name}");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Ready")
            readyCheckboxImage.gameObject.SetActive(false);
    }

}
