using System.Numerics;

namespace AKG.Core.Shadows;

/// <summary>
/// Axis-Aligned Bounding Box (AABB) для ускорения проверки пересечений лучей
/// </summary>
public struct AABB
{
    public Vector3 Min { get; }
    public Vector3 Max { get; }
    
    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }
    
    /// <summary>
    /// Проверяет пересечение луча с AABB (алгоритм Kay-Kajiya)
    /// </summary>
    /// <returns>True если луч пересекает бокс</returns>
    public bool IntersectRay(Ray ray)
    {
        float tmin = 0.0f;
        float tmax = float.MaxValue;

        for (int axis = 0; axis < 3; axis++)
        {
            float invDir = 1.0f / ray.Direction[axis];
            float t0 = (Min[axis] - ray.Origin[axis]) * invDir;
            float t1 = (Max[axis] - ray.Origin[axis]) * invDir;

            if (invDir < 0.0f)
                (t0, t1) = (t1, t0);

            tmin = MathF.Max(t0, tmin);
            tmax = MathF.Min(t1, tmax);

            if (tmax <= tmin)
                return false;
        }

        return true;
    }
}