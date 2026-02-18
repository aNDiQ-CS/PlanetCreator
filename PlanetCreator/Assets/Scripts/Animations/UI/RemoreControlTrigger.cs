using UnityEngine;

public class RemoreControlTrigger : MonoBehaviour
{
    [SerializeField] private TriggerManager m_triggerManager;

    void PlayTrigger(int trigger)
    {
        m_triggerManager.Trigger(trigger);
    }
}
