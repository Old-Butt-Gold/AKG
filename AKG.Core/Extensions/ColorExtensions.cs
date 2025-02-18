using System.Numerics;
using System.Windows.Media;
using AKG.Core.Objects;

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
    public static Color ApplyLambert(this Color baseColor, Vector3 normal, IEnumerable<Light> lambertLights)
    {
        float totalIntensity = 0f;
        foreach (var light in lambertLights)
        {
            // Нормализуем направление света
            Vector3 lightDir = Vector3.Normalize(light.Direction);
            // Вычисляем интенсивность для данного источника
            float intensity = MathF.Max(Vector3.Dot(normal, -lightDir), 0);
            // Если требуется учитывать коэффициент диффузного отражения (Kd) из настроек источника, можно умножить:
            // intensity *= light.Kd;
            totalIntensity += intensity;
        }

        // Ограничиваем суммарную интенсивность значением 1
        totalIntensity = MathF.Min(totalIntensity, 1.0f);

        return Color.FromArgb(
            baseColor.A,
            (byte)(baseColor.R * totalIntensity),
            (byte)(baseColor.G * totalIntensity),
            (byte)(baseColor.B * totalIntensity));
    }

    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
    }

    /// <summary>
    /// Вычисляет итоговый цвет фрагмента по модели Фонга с учётом нескольких источников света.
    /// Все входные векторы должны быть нормализованы.
    /// </summary>
    /// <param name="materialColor">Базовый цвет материала (в диапазоне 0..1 для каждого канала)</param>
    /// <param name="normal">Интерполированная нормаль в точке (единичный вектор)</param>
    /// <param name="viewDir">Вектор взгляда (обычно от фрагмента к камере)</param>
    /// <param name="lights">Коллекция источников света</param>
    /// <returns>Окончательный цвет фрагмента</returns>
    public static Color ApplyPhongShading(this Color materialColor,
        Vector3 normal, Vector3 viewDir, IEnumerable<Light> lights)
    {
        var ambient = Vector3.Zero;
        var diffuse = Vector3.Zero;
        var specular = Vector3.Zero;
        
        // Нормализуем (единичный вектор, чтобы потом и единичный light.Direction использовали)
        normal = Vector3.Normalize(normal);
        viewDir = Vector3.Normalize(viewDir);
        
        foreach (var light in lights)
        {
            // Преобразуем цвета источника в вектор с компонентами от 0 до 1
            
            // Фоновый цвет
            var ambientLight = light.Ambient.ToVector3();
            ambient += ambientLight * light.Ka;
            
            // Рассеяное освещение
            var diffuseLight = light.Diffuse.ToVector3();
            var normalizedLight = Vector3.Normalize(light.Direction);
            // Интенсивность диффузного света: max(N · L, 0)
            float NdotL = MathF.Max(Vector3.Dot(normal, normalizedLight), 0);
            diffuse += diffuseLight * NdotL * light.Kd;
            
            // Зеркальное освещение
            var specularLight = light.Specular.ToVector3();
            
            // Рефлексия: R = L - 2 · (N · L) · N
            Vector3 R = Vector3.Reflect(normalizedLight, normal);
            float RdotV = MathF.Max(Vector3.Dot(R, viewDir), 0);
            specular += specularLight * MathF.Pow(RdotV, light.Shininess) * light.Ks;
        }

        // Итоговая освещенность
        var result = ambient + diffuse + specular;
        result *= materialColor.ToVector3();
        result = Vector3.Clamp(result, Vector3.Zero, Vector3.One);

        return Color.FromArgb(255,
            (byte)(result.X * 255),
            (byte)(result.Y * 255),
            (byte)(result.Z * 255));
    }
}