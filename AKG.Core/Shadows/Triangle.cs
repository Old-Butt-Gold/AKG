using System.Numerics;

namespace AKG.Core.Shadows;


/// <summary>
/// Представляет треугольник для расчёта теней.
/// </summary>
public class Triangle
{
    public const float Bias = 0.0001f;
    
    public Vector3 V0, V1, V2;
    public Vector3 Center => (V0 + V1 + V2) / 3.0f;

    /// <summary>
    /// Проверка пересечения луча с треугольником (алгоритм Моллера-Трумбора)
    /// </summary>
    public bool Intersect(Ray ray, out float t)
    {
        t = 0f;
        var edge1 = V1 - V0;
        var edge2 = V2 - V0;
        
        // 1. Вычисление вектора нормали к плоскости
        var h = Vector3.Cross(ray.Direction, edge2);
        var a = Vector3.Dot(edge1, h);
        
        // 2. Проверка параллельности луча и плоскости
        if (MathF.Abs(a) < float.Epsilon)
            return false;

        var f = 1.0f / a;
        var s = ray.Origin - V0;
        
        // 3. Вычисление barycentric координаты u
        var u = f * Vector3.Dot(s, h);
        if (u is < 0 or > 1)
            return false;

        // 4. Вычисление barycentric координаты v
        var q = Vector3.Cross(s, edge1);
        var v = f * Vector3.Dot(ray.Direction, q);
        if (v < 0 || u + v > 1)
            return false;

        // 5. Вычисление расстояния до точки пересечения
        t = f * Vector3.Dot(edge2, q);
        return t > Bias; // Игнорируем пересечения за поверхностью
    }
}