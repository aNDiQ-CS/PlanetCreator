using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private Animator m_buttonAnimator;
    [SerializeField] private CanvasGroup m_buttons;

    public void FadeIn()
    {
        m_buttons.blocksRaycasts = true;
        m_buttons.interactable = true;
        m_buttonAnimator.SetBool("Fade", true);
    }

    public void FadeOut()
    {
        m_buttons.blocksRaycasts = false;
        m_buttons.interactable = false;
        m_buttonAnimator.SetBool("Fade", false);
    }
}