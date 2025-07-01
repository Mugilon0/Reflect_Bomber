using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIScreen : MonoBehaviour
{
    public bool isModal = false;
    public Selectable initialSelected;
    public UnityEvent onFocused;
    public UnityEvent onDefocused;
    [HideInInspector] public UIScreen previousScreen = null;

    CanvasGroup _group = null;
    public CanvasGroup Group
    {
        get
        {
            if (_group) return _group;
            return _group = GetComponent<CanvasGroup>();
        }
    }

    public static UIScreen activeScreen; // UIScreen.Focusでどこからでも画面を切り替えられる

    public static void Focus(UIScreen screen)
    {
        if (activeScreen)
            activeScreen.FocusScreen(screen);
        else
            screen.Focus();
    }

    public void Focus()
    {
        if (previousScreen)
            previousScreen.gameObject.SetActive(false);
        gameObject.SetActive(true); // 自身を表示する
        Group.interactable = true; // ボタンを操作可能にする
        activeScreen = this; 
        onFocused?.Invoke();
        if (initialSelected)
            initialSelected.Select();
    }

    public void Defocus()
    {
        onDefocused?.Invoke();
        Group.interactable = false;
        if (isModal == false)
            gameObject.SetActive(false);
    }

    void FocusScreen(UIScreen screen)
    {
        if (screen == this) return;

        Defocus();
        screen.previousScreen = this;
        screen.Focus();
    }

    public void Back()
    {
        if (previousScreen)
            previousScreen.FocusScreen(this);
    }

    public void BackTo(UIScreen screen)
    {
        if (activeScreen == screen) return;

        if (activeScreen && activeScreen.previousScreen)
        {
            activeScreen.Back();
        }

        if (activeScreen != screen)
        {
            Focus(screen);
        }
    }
}