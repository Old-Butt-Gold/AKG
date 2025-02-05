using System.Numerics;

namespace AKG.Parser;

/// <summary>
/// Класс модели, содержащей списки всех элементов 
/// </summary>
public class ObjModel
{
    // V
    // W – Дополнительная координата, по умолчанию 1
    public List<Vector4> Vertices { get; } = [];
    // Vt
    // V – Необязательная координата для двухмерной текстуры, по умолчанию 0
    // W – Необязательная координата для трехмерной текстуры, по умолчанию 0
    public List<Vector3> TextureCoords { get; } = [];
    // Vn
    // I – X
    // J – Y
    // K – Z
    public List<Vector3> Normals { get; } = [];
    
    // F/V/N список полигонов/граней
    public List<Face> Faces { get; } = [];
    
    // Минимальные координаты по X, Y, Z (bounding box)
    public Vector4 Min { get; set; }

    // Максимальные координаты по X, Y, Z (bounding box)
    public Vector4 Max { get; set; }

    // Коэффициент масштабирования, рассчитанный по размеру объекта.
    public float Scale { get; set; }

    // Дополнительная величина, например, для шага перемещения
    public float Delta { get; set; }
}