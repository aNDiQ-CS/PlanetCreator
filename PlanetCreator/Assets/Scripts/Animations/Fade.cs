using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Fade : MonoBehaviour
{
    [SerializeField] private GameObject[] m_buttons;
    [SerializeField] private float fadeSpeed = 2f;

    private float targetAlpha = 1f; // значение к которому стремится текущая непрозрачность
    private float currentAlpha = 1f; // текущая непрозрачность

    public void FadeIn() => targetAlpha = 1f;
    public void FadeOut() => targetAlpha = 0f;

    // чекаем есть ли разница между currentAlpha и targetAlpha
    private void Update()
    {
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
        {
            // плавно меняем нашу непрозрачность без резких скачков
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            ApplyAlphaToAll(currentAlpha);
        }
    }

    private void ApplyAlphaToAll(float alpha)
    {
        // устанавливаем рамки от 0 до 1
        alpha = Mathf.Clamp01(alpha);

        // проходимся по всем объектам массива
        foreach (var btn in m_buttons)
        {
            // проверка наличия элемента 
            if (!btn) continue;

            // (1) попытка получения указанного компонента и в случае успеха сохраняем в img
            if (btn.TryGetComponent<Image>(out var img))
                // смена прозрачности
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);

            // поиск компонента среди род. и доч. объектов и сохранение в text, если результат не null
            if (btn.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI text)
                // смена прзрачности текста
                text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);

            // аналогичноо (1)
            if (btn.TryGetComponent<Button>(out var button))
                // активность кнопки, но хз подойдёт ли такое значения порога её активности?
                button.interactable = alpha > 0.6f;
        }
    }
}