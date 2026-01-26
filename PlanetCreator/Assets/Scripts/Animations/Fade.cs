using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private Animator m_canvasAnimator;
    [SerializeField] private CanvasGroup m_buttons;

    public void FadeIn()
    {
        m_buttons.blocksRaycasts = true;
        m_canvasAnimator.SetBool("Fade", true);
    }

    public void FadeOut()
    {
        m_buttons.blocksRaycasts = false;
        //Debug.Log("Àó÷!");
        m_canvasAnimator.SetBool("Fade", false);
    }
}