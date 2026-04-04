using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Компонент на сцене, описывающий последовательность шагов уровня.
    /// Все ссылки — на объекты сцены, никаких ScriptableObject.
    /// </summary>
    public class LevelSequence : MonoBehaviour
    {
        [SerializeField] private LevelStep[] m_steps;

        public LevelStep[] Steps => m_steps;
        public int StepCount => m_steps != null ? m_steps.Length : 0;

        public LevelStep GetStep(int index)
        {
            if (m_steps == null || index < 0 || index >= m_steps.Length)
                return null;

            return m_steps[index];
        }
    }
}