using System;

namespace Infrastructure
{
    /// <summary>
    /// Интерфейс для любой мини-игры.
    /// ChemicalFlaskManager и будущие мини-игры реализуют его.
    /// </summary>
    public interface IMiniGame
    {
        /// <summary>
        /// Вызывается когда мини-игра завершена (игрок выполнил задание).
        /// </summary>
        event Action MiniGameCompleted;

        /// <summary>
        /// Запуск мини-игры. Вызывается из LevelFlowState.
        /// </summary>
        void StartGame();

        /// <summary>
        /// Остановка мини-игры (принудительная, при выходе).
        /// </summary>
        void StopGame();
    }
}