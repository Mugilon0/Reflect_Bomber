using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimplestarGame;

public class InterfaceManager : MonoBehaviour
{
    public Animator battleCountdownAnimator;
    public UIScreen readyChatUI;
    public UIScreen loadingUI;
    public UIScreen battleCountdownUI;
    public UIScreen inGameUIScreen;


    [SerializeField] internal TMPro.TMP_InputField readyMessageInputField;
    [SerializeField] internal ButtonPressDetection readyMessageButtonSend;

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
