using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DragAndDropLegacy : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_followSpeed = 35f;

    [Header("Auto Rotation Settings")]
    [SerializeField] private float m_rotationSpeed = 20f;
    [SerializeField] private float m_pourAngleZ = -110f;
    [SerializeField] private float m_rayDistance = 15f; // Увеличили для надежности
    [SerializeField] private LayerMask m_containerLayer; // Слой контейнера

    private Rigidbody m_rigidbody;
    private bool m_isDragging = false;
    private float m_zDistanceToCamera;
    private Vector3 m_targetWorldPos;

    private void OnEnable()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        HandleInput();
        HandleAutoRotation();
    }

    private void FixedUpdate()
    {
        if (m_isDragging)
        {
            // Плавное следование за курсором
            transform.position = Vector3.Lerp(transform.position, m_targetWorldPos, Time.fixedDeltaTime * m_followSpeed);
        }
    }

    private void StartDragging()
    {
        m_isDragging = true;
        m_zDistanceToCamera = Camera.main.WorldToScreenPoint(transform.position).z;

        // Обнуляем физику ДО включения кинематики
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;

        m_rigidbody.isKinematic = true;
        m_rigidbody.useGravity = false;

        UpdateTargetPosition();
    }

    private void EndDragging()
    {
        m_isDragging = false;
        m_rigidbody.isKinematic = false;
        m_rigidbody.useGravity = true;

        // Обнуляем физику ПОСЛЕ выключения кинематики (убирает ошибки)
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Проверяем попадание в колбу
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    StartDragging();
            }
        }

        if (m_isDragging)
        {
            UpdateTargetPosition();
            if (Input.GetMouseButtonUp(0)) EndDragging();
        }
    }

    private void UpdateTargetPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = m_zDistanceToCamera;
        m_targetWorldPos = Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void HandleAutoRotation()
    {
        Quaternion targetRotation = Quaternion.identity;

        if (m_isDragging)
        {
            // Пускаем луч строго вниз
            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;

            // Проверка попадания с визуализацией
            if (Physics.Raycast(ray, out hit, m_rayDistance, m_containerLayer))
            {
                // Если попали в объект со слоем контейнера
                Debug.DrawRay(transform.position, Vector3.down * m_rayDistance, Color.green);
                targetRotation = Quaternion.Euler(0, 0, m_pourAngleZ);
            }
            else
            {
                Debug.DrawRay(transform.position, Vector3.down * m_rayDistance, Color.red);
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * m_rotationSpeed);
    }
}