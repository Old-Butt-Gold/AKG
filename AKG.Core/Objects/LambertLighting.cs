using System.Numerics;
using System.Windows.Media;

namespace AKG.Core.Objects;

/// <summary>
/// Статический класс, отвечающий за модель освещения Ламберта.
/// Здесь задается направление источника света, а также реализована функция, возвращающая затененный цвет.
/// </summary>
public static class LambertLighting
{
    // Направление источника света (например, направлено сверху и немного сбоку)
    public static Vector3 LambertLight = -new Vector3(1, 1, 2);

    /// <summary>
    /// Применяет модель Ламберта к базовому цвету на основе нормали.
    /// Вычисляется интенсивность освещения как абсолютное значение скалярного произведения нормали и
    /// направления света (после нормализации). Итоговый цвет – базовый цвет, умноженный на интенсивность.
    /// </summary>
    public static Color ApplyLambert(Color baseColor, Vector3 normal)
    {
        // Нормализуем направление света
        Vector3 lightDir = Vector3.Normalize(LambertLight);
        // Интенсивность – косинус угла между нормалью и направлением света (от 0 до 1)
        float intensity = MathF.Max(Vector3.Dot(normal, -lightDir), 0);
        // Применяем интенсивность к каждому цветовому каналу (A остаётся неизменным)
        return Color.FromArgb(baseColor.A,
            (byte)(baseColor.R * intensity),
            (byte)(baseColor.G * intensity),
            (byte)(baseColor.B * intensity));
    }
}