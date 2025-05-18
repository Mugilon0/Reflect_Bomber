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
        isDoneWithCharacterSelection = isReady;
    }


    static void OnIsDoneWithCharacterSelectionChanged(Changed<CharacterOutfitHandler> changed)
    {
        changed.Behaviour.IsDoneWithCharacterSelectionChanged();
    }

    private void IsDoneWithCharacterSelectionChanged()
    {
        // キャラ変更して際トリガーされないための保険(?)
        if (SceneManager.GetActiveScene().name != "Ready")
            return;

        if (isDoneWithCharacterSelection)
            readyCheckboxImage.gameObject.SetActive(true);
        else readyCheckboxImage.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Ready")
            readyCheckboxImage.gameObject.SetActive(false);
    }

}
