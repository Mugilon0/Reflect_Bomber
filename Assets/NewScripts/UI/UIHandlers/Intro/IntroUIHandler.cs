using UnityEngine;

public class IntroUIHandler : MonoBehaviour
{
    [SerializeField] private Animator introAnimator;

    public void PlayCountdown()
    {
        introAnimator?.SetTrigger("StartCountdown");
    }
}
