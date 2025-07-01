using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HUD : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;


    public static HUD Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public static void SetTimerText(float time)
    {
        Instance.timeText.text = $"{(int)(time / 60f):00}:{time % 60:00.00}";
    }
}
