using UnityEngine;

/// <summary>
/// Вешается на префаб спутника.
/// Бесконечно вращает объект вокруг родителя (планеты).
/// </summary>
public class OrbitRotation : MonoBehaviour
{
    [SerializeField] private float m_speed = 30f;
    [SerializeField] private Vector3 m_axis = Vector3.up;

    private void Update()
    {
        transform.RotateAround(transform.parent.position, m_axis, m_speed * Time.deltaTime);
    }
}