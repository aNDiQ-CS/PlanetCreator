using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;

    public void PlayAudio()
    {
        m_audioSource.Play();
    }

    public void StopAudio()
    {
        m_audioSource.Stop();
    }
}
