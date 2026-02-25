using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class Stream : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Vector3 targetPosition;
    private Coroutine pourRoutine;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }

    // Метод для установки цвета струи
    public void SetColor(Color color)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        // Устанавливаем цвет начала и конца линии
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Если у вас используется специальный материал (например, HDR), 
        // можно также передать цвет напрямую в шейдер:
        // lineRenderer.material.color = color; 
    }

    public void StartStream()
    {
        pourRoutine = StartCoroutine(BeginPour());
    }

    public void StopStream()
    {
        if (pourRoutine != null) StopCoroutine(pourRoutine);
        Destroy(gameObject, 0.1f);
    }

    private IEnumerator BeginPour()
    {
        while (gameObject.activeSelf)
        {
            targetPosition = FindEndPoint();
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, targetPosition);
            yield return null;
        }
    }

    private Vector3 FindEndPoint()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out hit, 2.0f)) return hit.point;
        return ray.GetPoint(2.0f);
    }
}