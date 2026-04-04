using UnityEngine;

/// <summary>
/// Скрипт для управления анимациями персонажа через клавиши 1-9 и Q,W,E.
/// Предполагается, что в Animator есть целочисленный параметр (например, "AnimationIndex"),
/// который переключает между состояниями анимаций.
/// </summary>
public class AnimationByKeys : MonoBehaviour
{
    [Tooltip("Имя целочисленного параметра в Animator, отвечающего за выбор анимации")]
    [SerializeField] private string animParameterName = "AnimationIndex";

    private Animator animator;

    private void Start()
    {
        // Получаем компонент Animator на этом же объекте
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator не найден! Скрипт будет отключён.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // Проверка нажатия цифр 1-9
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i)) // Alpha1 = Alpha0+1, Alpha2 = Alpha0+2 и т.д.
            {
                SetAnimationIndex(i - 1); // Индексы 0..8 для цифр 1..9
                return; // Прерываем проверку, чтобы не срабатывали несколько клавиш за кадр
            }
        }

        // Проверка клавиш Q, W, E
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetAnimationIndex(9);  // Индекс 9 для Q
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            SetAnimationIndex(10); // Индекс 10 для W
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SetAnimationIndex(11); // Индекс 11 для E
        }
    }

    /// <summary>
    /// Устанавливает индекс анимации в Animator.
    /// </summary>
    /// <param name="index">Индекс от 0 до 11 включительно</param>
    private void SetAnimationIndex(int index)
    {
        // Можно добавить проверку на допустимый диапазон, если нужно
        if (index < 0 || index > 11)
        {
            Debug.LogWarning($"Индекс анимации {index} вне допустимого диапазона (0-11)");
            return;
        }

        animator.SetInteger(animParameterName, index);
        Debug.Log($"Переключение на анимацию с индексом {index}");
    }
}