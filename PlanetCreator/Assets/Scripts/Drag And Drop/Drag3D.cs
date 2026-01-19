using UnityEngine;

/// <summary>
/// Drag3D — drag & drop for 3D rigidbodies (mouse + touch).
/// Uses a physics anchor + FixedJoint to avoid clipping through colliders.
/// Attach to any manager object. Requires Camera.main to be present.
/// </summary>
public class Drag3D : MonoBehaviour
{
    [Tooltip("Слой(ы) для перетаскиваемых объектов")]
    public LayerMask draggableLayers = ~0;

    [Tooltip("Макс. расстояние луча для захвата")]
    public float maxPickDistance = 30f;

    [Tooltip("Скорость перемещения якоря (чем выше - тем жёстче)")]
    public float anchorMoveSpeed = 15f;

    [Tooltip("Использовать SpringJoint вместо FixedJoint для более мягкого эффекта")]
    public bool useSpringJoint = false;

    [Tooltip("Параметры SpringJoint (только если useSpringJoint=true)")]
    public float spring = 1000f;
    public float damper = 50f;

    // Внутренние
    private Camera cam;
    private Rigidbody grabbedRb;
    private Rigidbody anchorRb;
    private Joint activeJoint;
    private float grabDistance;      // расстояние от камеры до точки захвата
    private Vector3 grabHitPoint;    // локальная точка захвата на теле
    private int pointerId = -1;      // для touch: индекс пальца, для мыши: -1

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("Drag3D: Camera.main not found. Please set a camera tagged 'MainCamera'.");
    }

    void Update()
    {
        // Обработка мыши (первой) и тача (если есть)
        if (Input.touchCount > 0)
        {
            // Только первый активный touch поддерживаем
            Touch t = Input.GetTouch(0);
            HandleTouchPhase(t.fingerId, t.position, t.phase);
        }
        else
        {
            // Мышь
            if (Input.GetMouseButtonDown(0))
                TryBeginDrag(-1, Input.mousePosition);
            if (Input.GetMouseButton(0))
                UpdateDragPointer(-1, Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
                EndDrag();
        }
    }

    void FixedUpdate()
    {
        // Перемещаем якорь физически (MovePosition) — чтобы joint работал корректно
        if (anchorRb != null)
        {
            Vector3 target = GetPointerWorldPoint(pointerId);
            // Плавно (интерполяция) — можно убрать Lerp для более жёсткого поведения
            Vector3 next = Vector3.Lerp(anchorRb.position, target, anchorMoveSpeed * Time.fixedDeltaTime);
            anchorRb.MovePosition(next);
        }
    }

    // --- Input handlers ---

    private void HandleTouchPhase(int id, Vector2 screenPos, TouchPhase phase)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                TryBeginDrag(id, screenPos);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                UpdateDragPointer(id, screenPos);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (pointerId == id) EndDrag();
                break;
        }
    }

    private void TryBeginDrag(int id, Vector2 screenPos)
    {
        if (grabbedRb != null) return; // уже что-то держим

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxPickDistance, draggableLayers))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null)
            {
                // Если попали в Collider без Rigidbody, можно попытаться взять rigidbody из parent
                rb = hit.collider.GetComponentInParent<Rigidbody>();
            }
            if (rb != null && !rb.isKinematic)
            {
                BeginDrag(rb, hit, id);
            }
        }
    }

    private void UpdateDragPointer(int id, Vector2 screenPos)
    {
        if (grabbedRb == null) return;
        // просто обновляем pointerId и цель — actual movement происходит в FixedUpdate
        pointerId = id;
    }

    private void EndDrag()
    {
        if (grabbedRb == null) return;

        // Удаляем joint и anchor
        if (activeJoint != null)
            Destroy(activeJoint);
        activeJoint = null;

        if (anchorRb != null)
            Destroy(anchorRb.gameObject);
        anchorRb = null;

        // вернуть физические параметры при необходимости (если вы их меняли)
        grabbedRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        grabbedRb = null;
        pointerId = -1;
    }

    // --- Drag lifecycle ---

    private void BeginDrag(Rigidbody rb, RaycastHit hit, int id)
    {
        grabbedRb = rb;
        pointerId = id;
        grabDistance = hit.distance;
        grabHitPoint = rb.transform.InverseTransformPoint(hit.point);

        // Сохраняем и ставим более надёжный режим обнаружения столкновений
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Создаём якорь
        GameObject anchor = new GameObject("DragAnchor");
        anchor.transform.position = hit.point;
        anchorRb = anchor.AddComponent<Rigidbody>();
        anchorRb.mass = Mathf.Max(0.01f, rb.mass * 0.05f); // маленькая масса, но не ноль
        anchorRb.drag = 10f;
        anchorRb.angularDrag = 999f;
        anchorRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Не делаем kinematic — чтоб joint корректно взаимодействовал
        anchorRb.isKinematic = false;

        // Создаём joint
        if (useSpringJoint)
        {
            SpringJoint sj = grabbedRb.gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = anchorRb;
            sj.anchor = grabHitPoint;
            sj.autoConfigureConnectedAnchor = false;
            sj.connectedAnchor = Vector3.zero;
            sj.spring = spring;
            sj.damper = damper;
            sj.enableCollision = false;
            activeJoint = sj;
        }
        else
        {
            FixedJoint fj = grabbedRb.gameObject.AddComponent<FixedJoint>();
            fj.connectedBody = anchorRb;
            fj.anchor = grabHitPoint;
            fj.breakForce = Mathf.Infinity;
            fj.breakTorque = Mathf.Infinity;
            fj.enableCollision = false;
            activeJoint = fj;
        }
    }

    // Возвращает мировую позицию по текущему указателю (mouse/touch) на глубине grabDistance (вдоль луча)
    private Vector3 GetPointerWorldPoint(int id)
    {
        Vector3 screenPos;
        if (id >= 0 && Input.touchCount > 0)
        {
            // Найти touch с этим id (безопасно)
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == id)
                {
                    screenPos = Input.GetTouch(i).position;
                    Ray r = cam.ScreenPointToRay(screenPos);
                    return r.GetPoint(grabDistance);
                }
            }
            // Если не найден — вернуть текущую позицию якоря
            return anchorRb != null ? anchorRb.position : cam.transform.position + cam.transform.forward * grabDistance;
        }
        else
        {
            // Мышь
            screenPos = Input.mousePosition;
            Ray r = cam.ScreenPointToRay(screenPos);
            return r.GetPoint(grabDistance);
        }
    }
}
