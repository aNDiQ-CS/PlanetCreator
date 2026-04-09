using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragAndDrop : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float m_followSpeed = 35f;

    [Header("Auto Rotation Settings")]
    [SerializeField] private float m_rotationSpeed = 20f;
    [SerializeField] private float m_pourAngleZ = -110f;

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
            transform.position = Vector3.Lerp(
                transform.position, m_targetWorldPos,
                Time.fixedDeltaTime * m_followSpeed);
        }
    }

    private void StartDragging()
    {
        m_isDragging = true;
        m_zDistanceToCamera = Camera.main.WorldToScreenPoint(transform.position).z;

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

        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            Time.deltaTime * m_rotationSpeed);
    }
}