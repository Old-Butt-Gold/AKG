using System.Numerics;
using System.Windows.Media;

namespace AKG.Core.Extensions;

public static class ColorExtensions
{
    public static int ColorToIntBgra(this Color color)
    {
        return (color.B << 0) | (color.G << 8) | (color.R << 16) | (color.A << 24);
    }
    
    /// <summary>
    /// Применяет модель Ламберта к базовому цвету на основе нормали.
    /// Вычисляется интенсивность освещения как абсолютное значение скалярного произведения нормали и
    /// направления света (после нормализации). Итоговый цвет – базовый цвет, умноженный на интенсивность.
    /// </summary>
    public static Color ApplyLambert(this Color baseColor, Vector3 normal, Vector3 lambertLight)
    {
        // Нормализуем направление света
        Vector3 lightDir = Vector3.Normalize(lambertLight);
        // Интенсивность – косинус угла между нормалью и направлением света (от 0 до 1)
        float intensity = MathF.Max(Vector3.Dot(normal, -lightDir), 0);
        // Применяем интенсивность к каждому цветовому каналу (A остаётся неизменным)
        return Color.FromArgb(baseColor.A,
            (byte)(baseColor.R * intensity),
            (byte)(baseColor.G * intensity),
            (byte)(baseColor.B * intensity));
    }
}