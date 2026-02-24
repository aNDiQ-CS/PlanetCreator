using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineManager : MonoBehaviour
{
    [SerializeField] private PlayableDirector m_director;
    [SerializeField] private TriggerManager m_triggerManager;
    [SerializeField] private Animator m_cameraAnim;

    public void PlayTimeline()
    {
        m_director.Play();
    }

    public void FadeTimeline(int number)
    {
        m_triggerManager.Trigger(number);
    }

    public void StopTimeline()
    {
        m_director.Stop();
    }

    public void StandartCamera()
    {
        m_cameraAnim.SetBool("Standart", true);
    }
}
