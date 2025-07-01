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



    [Networked(OnChanged = nameof(OnIsDoneWithCharacterSelectionChanged))]
    public NetworkBool isDoneWithCharacterSelection { get; set; }




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
