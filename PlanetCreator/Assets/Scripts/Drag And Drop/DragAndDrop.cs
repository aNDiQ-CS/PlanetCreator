using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DragAndDrop — простой drag&drop с AddForce для мыши и touch.
/// Исправлена ошибка типов: теперь touch позиция конвертируется в Vector3 с корректным z.
/// Требования: объект должен иметь Collider и Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float m_force = 500f;

    // Смещение между позицией курсора (screen) и экранной позицией центра объекта
    private Vector3 m_mousePosition;
    private Rigidbody m_rigidbody;

    // Для touch: id пальца, который захватил объект. -1 — ничего не захвачено.
    private int m_touchId = -1;

    private void OnEnable()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private Vector3 GetMousePosition()
    {
        if (Camera.main == null)
        {
            Debug.LogError("DragAndDrop: Camera.main не найдена. Пометьте камеру тегом MainCamera.");
            return Vector3.zero;
        }
        // возвращаем экранную позицию объекта (x,y) и z = расстояние до камеры (нужно для ScreenToWorldPoint)
        return Camera.main.WorldToScreenPoint(transform.position);
    }

    #region Mouse handlers

    private void OnMouseDown()
    {
        // Сохраняем смещение (чтобы позиция захвата была корректной)
        m_mousePosition = Input.mousePosition - GetMousePosition();
    }

    private void OnMouseDrag()
    {
        if (Camera.main == null) return;

        Vector3 screenPoint = (Vector3)Input.mousePosition - m_mousePosition; // ввод как Vector3 (z будет ноль, но m_mousePosition.z учитывает)
        // Обеспечим корректный z: используем z из GetMousePosition()
        screenPoint.z = GetMousePosition().z;
        Vector3 cameraDrag = Camera.main.ScreenToWorldPoint(screenPoint);

        m_rigidbody.AddForce((cameraDrag - transform.position) * m_force, ForceMode.Force);
        m_rigidbody.velocity = Vector3.zero;
    }

    #endregion

    #region Touch handlers

    private void Update()
    {
        if (Input.touchCount == 0)
        {
            return;
        }

        // Обрабатываем все касания — нужен только тот, который захватил объект (m_touchId)
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            switch (t.phase)
            {
                case TouchPhase.Began:
                    TryBeginTouch(t);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (m_touchId == t.fingerId)
                        ContinueTouchDrag(t);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (m_touchId == t.fingerId)
                        EndTouchDrag();
                    break;
            }
        }
    }

    private void TryBeginTouch(Touch t)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(t.position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            if (hit.collider != null)
            {
                // Начинаем drag только если касание попало в этот коллайдер (или в дочерний)
                if (hit.collider.transform == this.transform || hit.collider.transform.IsChildOf(transform))
                {
                    m_touchId = t.fingerId;

                    // ВАЖНО: корректно формируем Vector3 для экранной точки и заливаем туда z из GetMousePosition()
                    Vector3 touchScreenPoint = new Vector3(t.position.x, t.position.y, GetMousePosition().z);
                    m_mousePosition = touchScreenPoint - GetMousePosition();
                }
            }
        }
    }

    private void ContinueTouchDrag(Touch t)
    {
        if (Camera.main == null) return;

        // Здесь исправлён момент: формируем Vector3 с тем же z, который использовался при захвате
        Vector3 touchScreenPoint = new Vector3(t.position.x, t.position.y, GetMousePosition().z);
        Vector3 screenPoint = touchScreenPoint - m_mousePosition;
        Vector3 cameraDrag = Camera.main.ScreenToWorldPoint(screenPoint);

        m_rigidbody.AddForce((cameraDrag - transform.position) * m_force, ForceMode.Force);
        m_rigidbody.velocity = Vector3.zero;
    }

    private void EndTouchDrag()
    {
        m_touchId = -1;
    }

    #endregion
}
