using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterfaceManager : MonoBehaviour
{
    public Animator battleCountdownAnimator;
    public UIScreen inGameUIScreen;


    public static InterfaceManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //sessionSetup.Init();
    }

    public void StartCountdown()
    {
        Debug.Log("Battleカウントダウンを開始します");
        battleCountdownAnimator.SetTrigger("StartCountdown");
    }

}
