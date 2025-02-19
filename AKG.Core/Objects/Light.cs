using System.Numerics;
using System.Windows.Media;
using AKG.Core.Extensions;

namespace AKG.Core.Objects;

public class Light
{
    /// <summary>
    /// Направление источника света (не нормализовано).
    /// </summary>
    public Vector3 Direction { get; set; } = new(1, 1, 2);

    /// <summary>
    /// Амбиентная компонента освещения.
    /// </summary>
    public Color Ambient { get; set; } = Colors.White;

    /// <summary>
    /// Диффузная компонента освещения.
    /// </summary>
    public Color Diffuse { get; set; } = Colors.Gray;

    /// <summary>
    /// Зеркальная компонента освещения.
    /// </summary>
    public Color Specular { get; set; } = Colors.White;

    /// <summary>
    /// Коэффициент фонового (амбиентного) освещения
    /// </summary>
    public float Ka { get; set; } = 0.1f;
    
    /// <summary>
    /// Коэффициент рассеянного (диффузного) освещения
    /// </summary>
    public float Kd { get; set; } = 1.0f;
    
    /// <summary>
    /// Коэффициент зеркального освещения
    /// </summary>
    public float Ks { get; set; } = 0.2f;
    
    /// <summary>
    /// Показатель блеска поверхности.
    /// </summary>
    public float Shininess { get; set; } = 32f;

    /// <summary>
    /// Вычисляет итоговый цвет фрагмента по модели Фонга с учетом нескольких источников света.
    /// </summary>
    /// <param name="lights">Коллекция источников света.</param>
    /// <param name="normal">Нормаль в точке (единичный вектор).</param>
    /// <param name="viewDirection">Вектор взгляда (направление от фрагмента к камере, нормализованный).</param>
    /// <param name="fragWorld">Мировая позиция фрагмента (используется для расчета векторов).</param>
    /// <returns>Итоговый цвет фрагмента.</returns>
    public static Color ApplyPhongShading(List<Light> lights, Vector3 normal, 
        Vector3 viewDirection, Vector3 fragWorld)
    {
        // Инициализация компонент освещения:
        // - ambient: фоновое освещение
        // - diffuse: диффузное освещение
        // - specular: зеркальное (спекулярное) освещение
        var ambient = Vector3.Zero;
        var diffuse = Vector3.Zero;
        var specular = Vector3.Zero;
        
        foreach (var light in lights)
        {
            // 1. Амбиентная компонента
            ambient += light.Ambient.ToVector3() * light.Ka;
            
            // 2. Диффузная компонента
            // Вычисляем направление от фрагмента к источнику света и нормализуем его
            // Нормализация необходима для корректного расчета углов между векторами
            var lightDir = Vector3.Normalize(light.Direction - fragWorld);
            var NdotL = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            diffuse += light.Diffuse.ToVector3() * NdotL * light.Kd;
            
            /*var reflection = lightDir - 2 * Vector3.Dot(lightDir, normal) * normal;

            float rv = Vector3.Dot(-reflection, viewDirection);
            if (rv < 0)
            {
                rv = 0;
            }

            specular += light.Ks * MathF.Pow(Math.Abs(rv), light.Shininess) * light.Specular.ToVector3();
            */
            
            // 3. Зеркальная (спекулярная) компонента
            // Вычисляем вектор отражения света относительно нормали поверхности
            // Встроенный метод Vector3.Reflect выполняет расчет по формуле:
            // R = L - 2 * (L · N) * N, где L — направление света, N — нормаль
            
            // P.S. (из методички) Чтобы вычислить вектор отражения,
            // нужно отразить направление света относительно вектора нормали.
            Vector3 reflection = Vector3.Reflect(-lightDir, normal); 
            
            // Скалярное произведение между вектором отражения и вектором взгляда
            // MathF.Max используется для ограничения значения нулем, чтобы избежать отрицательных значений
            float RdotV = MathF.Max(Vector3.Dot(reflection, viewDirection), 0);
            
            // Если угол между вектором отражения и вектором взгляда положительный,
            // добавляем спекулярную компоненту, возведенную в степень Shininess
            if (RdotV > 0)
            {
                specular += light.Specular.ToVector3() * light.Ks * (float)Math.Pow(RdotV, light.Shininess);
            }
        }
        
        // Суммируем все компоненты освещения
        Vector3 phong = ambient + diffuse + specular;

        // Клипаем значения цвета в диапазон [0, 255]
        phong = Vector3.Clamp(phong, Vector3.Zero, new Vector3(255, 255, 255));

        // Возвращаем итоговый цвет
        return Color.FromArgb(255, (byte)phong.X, (byte)phong.Y, (byte)phong.Z);
    }
    
}