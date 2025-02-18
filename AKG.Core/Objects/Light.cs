using System.Numerics;
using System.Windows.Media;

namespace AKG.Core.Objects;

public class Light
{
    /// <summary>
    /// Если IsDirectional == true, то используется Direction, иначе Position.
    /// </summary>
    public bool IsDirectional { get; set; } = true;
    
    /// <summary>
    /// Направление для направленного источника (например, солнечный свет)
    /// </summary>
    public Vector3 Direction { get; set; } = Vector3.Normalize(new (-1, -1, -2));
    
    /// <summary>
    /// Позиция для точечного источника
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    // Цвета и интенсивности компонентов освещения
    public Color Ambient { get; set; } = Colors.Gray;
    public Color Diffuse { get; set; } = Colors.White;
    public Color Specular { get; set; } = Colors.White;

    // Коэффициенты материала для данного света (можно сделать глобальными, если требуется)
    public float Ka { get; set; } = 1.0f; //0.1f; // фоновое
    public float Kd { get; set; } = 1.0f; //0.7f; // рассеянное
    public float Ks { get; set; } = 1.0f; //0.2f; // зеркальное
    public float Shininess { get; set; } = 1.0f;  //32f; // коэффициент блеска поверхности
}