using UnityEngine;

public class UIToggle : MonoBehaviour
{
    [SerializeField] private GameObject uiObject;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            uiObject.SetActive(!uiObject.activeSelf);
        }
    }
}