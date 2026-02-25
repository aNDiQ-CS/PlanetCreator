using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DragAndDrop : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private float m_force = 600f;
    [SerializeField] private float m_damping = 10f; // Для плавности остановки

    [Header("Depth & Alignment (Выравнивание)")]
    public bool lockToPlane = true;       // Включить фиксацию в плоскости
    public bool autoDetectPlane = true;    // Авто-выбор оси (X или Z) в зависимости от камеры
    public float fixedCoordinate = 0f;     // Ручная координата (если autoDetectPlane выключен)

    [Tooltip("Если объект с этим тегом есть в сцене, возьмем его Z")]
    public string containerTag = "Container";

    private Vector3 m_mouseOffset;
    private Rigidbody m_rigidbody;
    private int m_touchId = -1;
    private float m_initialZDistance;

    private void OnEnable()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        // Настраиваем Rigidbody для более стабильного перетаскивания
        m_rigidbody.drag = m_damping;
        m_rigidbody.angularDrag = 5f;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        // Используем дистанцию от объекта до камеры как глубину
        mousePoint.z = m_initialZDistance;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    #region Mouse Handlers
    private void OnMouseDown()
    {
        if (Camera.main == null) return;

        // Определяем расстояние до камеры в момент захвата
        m_initialZDistance = Camera.main.WorldToScreenPoint(transform.position).z;
        m_mouseOffset = transform.position - GetMouseWorldPos();

        // Если нужно выровнять по контейнеру в момент захвата
        if (lockToPlane)
        {
            ApplyPlaneConstraint();
        }
    }

    private void OnMouseDrag()
    {
        MoveObject(GetMouseWorldPos() + m_mouseOffset);
    }
    #endregion

    #region Touch Handlers
    // Логика для мобильных устройств (упрощенная версия)
    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                    {
                        m_touchId = touch.fingerId;
                        m_initialZDistance = Camera.main.WorldToScreenPoint(transform.position).z;
                        m_mouseOffset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, m_initialZDistance));
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && touch.fingerId == m_touchId)
            {
                Vector3 touchPos = new Vector3(touch.position.x, touch.position.y, m_initialZDistance);
                MoveObject(Camera.main.ScreenToWorldPoint(touchPos) + m_mouseOffset);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                m_touchId = -1;
            }
        }
    }
    #endregion

    private void MoveObject(Vector3 targetPos)
    {
        if (lockToPlane)
        {
            // Решаем, какую ось фиксировать на основе направления камеры
            Vector3 camForward = Camera.main.transform.forward;

            if (autoDetectPlane)
            {
                // Если камера смотрит больше "вперед-назад" (Z), фиксируем Z
                if (Mathf.Abs(camForward.z) > Mathf.Abs(camForward.x))
                {
                    targetPos.z = fixedCoordinate;
                }
                else // Если камера смотрит "сбоку" (X), фиксируем X
                {
                    targetPos.x = fixedCoordinate;
                }
            }
            else
            {
                targetPos.z = fixedCoordinate;
            }
        }

        // Применяем силу к Rigidbody для движения к целевой точке
        Vector3 force = (targetPos - transform.position) * m_force;
        m_rigidbody.AddForce(force - m_rigidbody.velocity * m_damping);
    }

    private void ApplyPlaneConstraint()
    {
        // 1. Пытаемся найти контейнер, чтобы взять его координату
        GameObject container = GameObject.FindGameObjectWithTag(containerTag);
        if (container != null)
        {
            // Определяем, по какой оси выравнивать (по Z или по X)
            Vector3 camForward = Camera.main.transform.forward;
            if (Mathf.Abs(camForward.z) > Mathf.Abs(camForward.x))
                fixedCoordinate = container.transform.position.z;
            else
                fixedCoordinate = container.transform.position.x;
        }

        // 2. Мгновенно подтягиваем колбу к нужной плоскости
        Vector3 currentPos = transform.position;
        Vector3 camFwd = Camera.main.transform.forward;

        if (Mathf.Abs(camFwd.z) > Mathf.Abs(camFwd.x))
            currentPos.z = fixedCoordinate;
        else
            currentPos.x = fixedCoordinate;

        transform.position = currentPos;

        // 3. Замораживаем вращение по осям, чтобы колба не заваливалась при перемещении 
        // (Wobble сам будет управлять вращением, когда нужно)
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }
}