using UnityEngine;

/// <summary>
/// Утилита для плавных анимаций.
/// </summary>
public static class Easing
{
    /// <summary>
    /// Ease-in-out: плавное начало и плавное окончание.
    /// t от 0 до 1, возвращает от 0 до 1.
    /// </summary>
    public static float InOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}