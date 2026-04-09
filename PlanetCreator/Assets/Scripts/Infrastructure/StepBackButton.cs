using Infrastructure;
using Infrastructure.States;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер кнопки "Назад".
/// Привязать к кнопке на Canvas. При нажатии возвращает LevelFlowState на предыдущий шаг.
/// Автоматически скрывает/показывает кнопку в зависимости от того, можно ли вернуться.
/// </summary>
public class StepBackButton : MonoBehaviour
{
    [SerializeField] private StateMachine m_stateMachine;
    [SerializeField] private Button m_button;
    [SerializeField] private GameObject m_buttonVisual; // Опционально: объект для скрытия кнопки

    private void Awake()
    {
        if (m_button == null)
            m_button = GetComponent<Button>();

        if (m_button != null)
            m_button.onClick.AddListener(OnBackPressed);
    }

    private void OnEnable()
    {
        UpdateVisibility();
    }

    private void Update()
    {
        UpdateVisibility();
    }

    private void OnBackPressed()
    {
        if (m_stateMachine == null) return;

        // Проверяем что мы в LevelFlowState
        if (m_stateMachine.CurrentStateType != typeof(LevelFlowState))
            return;

        var levelFlow = m_stateMachine.GetState<LevelFlowState>();
        if (levelFlow == null) return;

        if (levelFlow.CanStepBack())
        {
            levelFlow.StepBack();
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        if (m_stateMachine == null) return;

        bool show = false;

        if (m_stateMachine.CurrentStateType == typeof(LevelFlowState))
        {
            var levelFlow = m_stateMachine.GetState<LevelFlowState>();
            show = levelFlow != null && levelFlow.CanStepBack();
        }

        if (m_buttonVisual != null)
            m_buttonVisual.SetActive(show);

        if (m_button != null)
            m_button.interactable = show;
    }
}
