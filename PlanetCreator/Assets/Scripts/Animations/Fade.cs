using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private Animator m_canvasAnimator;

    public void FadeIn()
    {
        m_canvasAnimator.SetBool("fade", true);
    }

    public void FadeOut()
    {
        m_canvasAnimator.SetBool("fade", false);
    }
}
