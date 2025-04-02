namespace AKG.Core.Shadows;

/// <summary>
/// Узел иерархии ограничивающих объемов
/// (Bounding Volume Hierarchy)
/// </summary>
public class BvhNode
{
    public AABB Bounds; // Ограничивающий объём узла
    public BvhNode? Left; // Левый дочерний узел
    public BvhNode? Right; // Правый дочерний узел
    public List<Triangle>? Triangles; // Треугольники (только в листьях)
    public bool IsLeaf => Triangles != null;
}