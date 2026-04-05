using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class WaypointMover : MonoBehaviour
{
    [Header("Маршрут (точки)")]
    [SerializeField] private List<WaypointData> m_waypoints = new List<WaypointData>();

    [Header("Настройки движения")]
    [SerializeField] private float m_moveSpeed = 2f;
    [SerializeField] private float m_rotationSpeed = 180f; // градусов в секунду

    [Header("Target для поворота после остановки")]
    [SerializeField] private Transform m_targetToLookAt; // сюда можно перетащить камеру или игрока

    // Кэш
    private Animator m_animator;
    private Coroutine m_activeMoveRoutine;

    // Состояния
    private bool m_isMoving = false;
    private Vector3[] m_curvePoints; // контрольные точки для сплайна (будет заполняться динамически)

    private void Start()
    {
        m_animator = GetComponent<Animator>();
        if (m_animator == null)
            Debug.LogWarning("Animator не найден – анимации не будут воспроизводиться");
    }

    /// <summary>
    /// Переместиться к точке маршрута по индексу с заданными анимациями.
    /// </summary>
    /// <param name="waypointIndex">Индекс точки назначения</param>
    /// <param name="animStandUp">Индекс анимации вставания (например, 2)</param>
    /// <param name="animWalk">Индекс анимации ходьбы (например, 1)</param>
    /// <param name="animSitDown">Индекс анимации усаживания (например, 3)</param>
    public void MoveToWaypoint(int waypointIndex, int animStandUp, int animSitDown)
    {
        if (waypointIndex < 0 || waypointIndex >= m_waypoints.Count)
        {
            Debug.LogError($"Индекс {waypointIndex} вне диапазона маршрута!");
            return;
        }

        if (m_activeMoveRoutine != null)
            StopCoroutine(m_activeMoveRoutine);

        m_activeMoveRoutine = StartCoroutine(MoveRoutine(waypointIndex, animStandUp, animSitDown));
    }

    private IEnumerator MoveRoutine(int targetIdx, int animStandUp, int animSitDown)
    {
        m_isMoving = true;

        // 1. Анимация вставания
        if (m_animator != null)
            m_animator.SetInteger("AnimationIndex", animStandUp);
        // Даём время на анимацию (можно подстроить под длительность анимации)
        // Лучше получить длину клипа из Animator, но для простоты – ждём 0.5 сек
        yield return new WaitForSeconds(0.5f);

        // 2. Построение криволинейного пути от текущей позиции до целевой точки
        Vector3 startPos = transform.position;
        Vector3 endPos = GetWaypointPosition(targetIdx);

        // Генерируем промежуточные точки для плавной кривой (CatmullRom)
        List<Vector3> curve = GenerateCurve(startPos, endPos, targetIdx);
        float totalDistance = GetCurveLength(curve);
        float moveDuration = totalDistance / m_moveSpeed;
        float elapsed = 0f;

        // Движение без изменения поворота (сохраняем текущий поворот)
        Quaternion originalRotation = transform.rotation;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            Vector3 pos = GetCurvePointAtT(curve, t);
            transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Фиксация в конечной точке
        transform.position = endPos;

        // 3. Остановка и поворот к целевой точке (например, к камере)
        if (m_targetToLookAt != null)
        {
            Vector3 directionToTarget = (m_targetToLookAt.position - transform.position).normalized;
            directionToTarget.y = 0; // игнорируем наклон по Y
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                float rotationDuration = Vector3.Angle(transform.forward, directionToTarget) / m_rotationSpeed;
                float rotElapsed = 0f;
                Quaternion startRot = transform.rotation;
                while (rotElapsed < rotationDuration)
                {
                    float rt = rotElapsed / rotationDuration;
                    transform.rotation = Quaternion.Slerp(startRot, targetRotation, rt);
                    rotElapsed += Time.deltaTime;
                    yield return null;
                }
                transform.rotation = targetRotation;
            }
        }

        // 4. Анимация усаживания
        if (m_animator != null)
            m_animator.SetInteger("AnimationIndex", animSitDown);
        yield return new WaitForSeconds(0.5f);

        m_isMoving = false;
        m_activeMoveRoutine = null;
    }

    // Получить позицию точки по индексу (учитывая Transform или Vector3)
    private Vector3 GetWaypointPosition(int idx)
    {
        WaypointData data = m_waypoints[idx];
        if (data.pointTransform == null)
        {
            Debug.LogError($"Waypoint {idx} не имеет ссылки на Transform! Проверь инспектор.");
            return transform.position; // или Vector3.zero, но лучше вернуть текущую позицию как fallback
        }
        return data.pointTransform.position;
    }

    // Генерация кривой CatmullRom через текущую позицию, точку назначения и одну промежуточную (для плавности)
    private List<Vector3> GenerateCurve(Vector3 start, Vector3 end, int targetIdx)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(start);

        // Добавляем промежуточные точки: можно использовать соседние waypoint'ы для изгиба
        // Для простоты – добавим точку в середине со смещением в случайную сторону (или по нормали)
        Vector3 mid = (start + end) / 2f;
        Vector3 dir = (end - start).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
        mid += perp * 1.5f; // отступ в сторону для кривизны
        points.Add(mid);
        points.Add(end);

        return points;
    }

    private float GetCurveLength(List<Vector3> curve)
    {
        float len = 0f;
        for (int i = 0; i < curve.Count - 1; i++)
            len += Vector3.Distance(curve[i], curve[i + 1]);
        return len;
    }

    private Vector3 GetCurvePointAtT(List<Vector3> curve, float t)
    {
        // Простая линейная интерполяция по ломаной (можно заменить на сплайн Безье)
        if (curve.Count == 2)
            return Vector3.Lerp(curve[0], curve[1], t);
        // Для большего числа точек – интерполируем по сегментам
        float totalLen = GetCurveLength(curve);
        float targetLen = t * totalLen;
        float accumulated = 0f;
        for (int i = 0; i < curve.Count - 1; i++)
        {
            float segLen = Vector3.Distance(curve[i], curve[i + 1]);
            if (accumulated + segLen >= targetLen)
            {
                float segmentT = (targetLen - accumulated) / segLen;
                return Vector3.Lerp(curve[i], curve[i + 1], segmentT);
            }
            accumulated += segLen;
        }
        return curve[curve.Count - 1];
    }

    public void Go()
    {
        MoveToWaypoint(0, 1, 2);
    }
}

[System.Serializable]
public class WaypointData
{
    public Transform pointTransform;
    public bool isStopPoint = false;
}