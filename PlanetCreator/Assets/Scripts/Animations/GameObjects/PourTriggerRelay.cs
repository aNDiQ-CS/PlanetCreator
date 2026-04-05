using UnityEngine;

/// <summary>
/// Вешается на родительский объект колбы (там же где Rigidbody).
/// Пробрасывает OnTriggerEnter/Exit в Wobble на дочернем объекте.
/// 
/// Unity отправляет trigger-события на объект с Rigidbody,
/// а не на дочерний — поэтому нужен этот мост.
/// </summary>
public class PourTriggerRelay : MonoBehaviour
{
    private Wobble m_wobble;

    private void Awake()
    {
        m_wobble = GetComponentInChildren<Wobble>();

        if (m_wobble == null)
            Debug.LogWarning($"[PourTriggerRelay] Wobble не найден в дочерних объектах {gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_wobble != null)
            m_wobble.OnPourZoneEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_wobble != null)
            m_wobble.OnPourZoneExit(other);
    }
}