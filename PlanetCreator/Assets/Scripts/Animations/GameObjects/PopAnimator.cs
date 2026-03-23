using UnityEngine;
using System.Collections;

public class PopAnimator : MonoBehaviour
{
    [Header("Размеры")]
    [SerializeField] private Vector3 m_endScale = Vector3.one;      // Конечный размер
    [SerializeField] private float m_excessMultiplier = 1.2f;       // Во сколько раз перепрыгиваем

    [Header("Времена (секунды)")]
    [SerializeField][Tooltip("Время на изменение scale от 0 до увеличенного размера")][Range(0, 5)] private float m_phase1Duration;                // От старта до переувеличения(overshoot)
    [SerializeField][Tooltip("Время на изменение scale от увеличенного размера до 1")][Range(0, 5)] private float m_phase2Duration;                // От переувеличения(overshoot) до конечного размера(target)

    [Header("Коллайдер и физика")]
    [SerializeField] private Collider m_col;
    [SerializeField] private Rigidbody m_rb;

    private Coroutine m_currentAnim;
    private float m_epsilon = 0.01f;

    void Awake()
    {
        if (m_col == null) m_col = GetComponent<Collider>();
        if (m_rb == null) m_rb = GetComponent<Rigidbody>();

        PopUp();
    }

    public void PopUp()
    {
        if (m_currentAnim != null) return;
        if (Vector3.Distance(transform.localScale, m_endScale) < m_epsilon) return;
        m_currentAnim = StartCoroutine(PopUpAnimation());
    }

    public void PopDown()
    {
        if (m_currentAnim != null) return;
        if (transform.localScale.magnitude < m_epsilon) return;
        m_currentAnim = StartCoroutine(PopDownAnimation());
    }

    private IEnumerator PopUpAnimation()
    {
        bool wasKinematic = false;
        if (m_rb != null)
        {
            wasKinematic = m_rb.isKinematic;
            m_rb.isKinematic = true;
        }

        if (m_col != null) m_col.enabled = true;

        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        Vector3 overshootScale = m_endScale * m_excessMultiplier;

        while (elapsed < m_phase1Duration)
        {
            float t = elapsed / m_phase1Duration;
            transform.localScale = Vector3.Lerp(Vector3.zero, overshootScale, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = overshootScale;

        elapsed = 0f;
        while (elapsed < m_phase2Duration)
        {
            float t = elapsed / m_phase2Duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(overshootScale, m_endScale, smoothT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = m_endScale;

        if (m_rb != null) m_rb.isKinematic = wasKinematic;

        m_currentAnim = null;
    }

    private IEnumerator PopDownAnimation()
    {
        bool wasKinematic = false;
        if (m_rb != null)
        {
            wasKinematic = m_rb.isKinematic;
            m_rb.isKinematic = true;
        }

        Vector3 startScale = transform.localScale;
        Vector3 overshootScale = m_endScale * m_excessMultiplier;

        float elapsed = 0f;
        while (elapsed < m_phase2Duration)
        {
            float t = elapsed / m_phase2Duration;
            transform.localScale = Vector3.Lerp(startScale, overshootScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = overshootScale;

        elapsed = 0f;
        while (elapsed < m_phase1Duration)
        {
            float t = elapsed / m_phase1Duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(overshootScale, Vector3.zero, smoothT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;

        if (m_col != null) m_col.enabled = false;

        if (m_rb != null) m_rb.isKinematic = wasKinematic;

        m_currentAnim = null;
    }
}