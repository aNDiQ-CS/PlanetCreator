using UnityEngine;
using Planets;

/// <summary>
/// Обработчик клика/тапа по колбе.
/// Автоматически добавляется ChemicalFlaskManager на каждую заспавненную колбу.
/// При клике уведомляет менеджер, который запускает анимацию наливания.
/// </summary>
public class FlaskClickHandler : MonoBehaviour
{
    private ChemicalType m_type;
    private ChemicalFlaskManager m_manager;
    private Camera m_camera;

    public void Initialize(ChemicalType type, ChemicalFlaskManager manager)
    {
        m_type = type;
        m_manager = manager;
    }

    private void Start()
    {
        m_camera = Camera.main;
    }

    private void Update()
    {
        if (m_manager == null || m_camera == null)
            return;

        // Обработка тапа (мобильные) и клика (десктоп)
        bool inputDown = false;
        Vector3 inputPosition = Vector3.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDown = true;
            inputPosition = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputDown = true;
            inputPosition = Input.mousePosition;
        }

        if (!inputDown) return;

        Ray ray = m_camera.ScreenPointToRay(inputPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Проверяем, что попали именно в этот объект или его дочерний
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform) ||
                transform.IsChildOf(hit.collider.transform))
            {
                m_manager.OnFlaskClicked(gameObject, m_type);
            }
        }
    }
}
