using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineManager : MonoBehaviour
{
    [SerializeField] private PlayableDirector m_director;
    [SerializeField] private TriggerManager m_triggerManager;
    [SerializeField] private Animator m_cameraAnim;

    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;
    private void OnEnable()
    {
        if (m_director != null)
            m_director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (m_director != null)
            m_director.stopped -= OnTimelineStopped;
    }

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

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);

        foreach (var obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
