using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    [SerializeField] private float m_speedX = 0f;
    [SerializeField] private float m_speedY = 10f;
    [SerializeField] private float m_speedZ = 0f;

    private void Update()
    {
        transform.Rotate(
            m_speedX * Time.deltaTime,
            m_speedY * Time.deltaTime,
            m_speedZ * Time.deltaTime,
            Space.Self
        );
    }
}