using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuUIHandler : MonoBehaviour
{
    public TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerNickname"))
            inputField.text = PlayerPrefs.GetString("PlayerNickName");
    }

    public void OnYourNameOkButtonClicked()
    {
        // player‚Ìnickname‚ð•Û‘¶
        PlayerPrefs.SetString("PlayerNickName", inputField.text);
        PlayerPrefs.Save();

        SceneManager.LoadScene("World1");
    }

}
