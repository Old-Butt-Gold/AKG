namespace AKG.Core.Shadows;

/// <summary>
/// Класс для работы с тенями и трассировкой лучей.
/// </summary>
public static class ShadowHelper
{
    /// <summary>
    /// Проверяет пересечение луча с BVH-деревом
    /// </summary>
    public static bool RayIntersectBvh(BvhNode node, Ray ray, out float closestT)
    {
        closestT = float.MaxValue;

        // Если луч не пересекает AABB узла — выходим
        if (!node.Bounds.IntersectRay(ray))
            return false;

        // Если это лист — проверяем пересечение с треугольниками
        if (node.IsLeaf)
        {
            bool hit = false;
            foreach (var tri in node.Triangles!)
            {
                if (tri.Intersect(ray, out var t) && t < closestT)
                {
                    closestT = t;
                    hit = true;
                }
            }

            return hit;
        }

        // Рекурсивно проверяем дочерние узлы
        bool leftHit = RayIntersectBvh(node.Left!, ray, out float leftT);
        bool rightHit = RayIntersectBvh(node.Right!, ray, out float rightT);

        closestT = Math.Min(leftT, rightT);
        return leftHit || rightHit;
    }
}