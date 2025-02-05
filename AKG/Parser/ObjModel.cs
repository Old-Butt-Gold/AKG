using System.Drawing;
using System.Numerics;
using AKG.VectorTransformations;

namespace AKG.Parser;

/// <summary>
/// Класс модели, содержащей списки всех элементов 
/// </summary>
public class ObjModel
{
    // Список исходных (оригинальных) вершин, полученных из файла OBJ.
    // V
    // W – Дополнительная координата, по умолчанию 1
    public List<Vector4> OriginalVertices { get; } = [];
    
    // Список вершин, которые будут использоваться для отображения (после применения преобразований).
    // Этот список обновляется в методе UpdateImage.
    public Vector4[] TransformedVertices { get; set; } = [];

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
    
    // Bounding box (минимальные и максимальные координаты по X, Y, Z)
    public Vector4 Min { get; set; }
    public Vector4 Max { get; set; }

    // Коэффициент масштабирования, рассчитанный по размеру объекта.
    public float Scale { get; set; }

    // Дополнительная величина, например, для шага перемещения
    public float Delta { get; set; }
    
    //Размер экрана
    public Size WindowSize { get; set; } = new(1080, 720);

    // Параметры камеры:
    
    // Позиция камеры в мировом пространстве
    public Vector3 Eye { get; init; } = new(1.0f, 1.0f, -MathF.PI);
    
    // Позиция цели, на которую направлена камера
    // направлена в центр сцены
    public Vector3 Target { get; init; } = Vector3.Zero;
    
    // Вектор, направленный вертикально вверх с точки зрения камеры
    // Вектор вверх (ось Y)
    public Vector3 Up { get; init; } = Vector3.UnitY;
    
    // Поле зрения камеры по оси Y (в радианах)
    public float Fov { get; init; } = MathF.PI / 3.0f; // 60° = PI / 3
    
    // Соотношение сторон обзора камеры
    public float Aspect { get; init; } = 16f / 9f;

    // Расстояние до ближней плоскости обзора
    public float ZNear { get; init; } = 0.1f;
    
    // Расстояние до дальней плоскости обзора
    public float ZFar { get; init; } = 100.0f;
    
    /// <summary>
    /// Обновляет отображаемые (трансформированные) вершины.
    /// Исходно копирует данные из OriginalVertices, затем последовательно
    /// применяет преобразования: мировое -> вид -> проекция -> viewport.
    /// </summary>
    public void UpdateImage()
    {
        // Start point to change TransformedVertices
        var worldTransform = Transformations.CreateWorldTransform(Scale, Matrix4x4.Identity, Vector3.Zero);
        this.ApplyWorldTransformation(worldTransform);

        var viewTransform = Transformations.CreateViewMatrix(Eye, Target, Up);
        this.ApplyViewTransformation(viewTransform);

        var projectionTransform = Transformations.CreatePerspectiveProjection(Fov, Aspect, ZNear, ZFar);
        this.ApplyTransformationProjection(projectionTransform);

        var viewportTransform = Transformations.CreateViewportMatrix(WindowSize.Width, WindowSize.Height);
        this.ApplyViewportTransformation(viewportTransform);
    }
}