using System.Drawing;
using System.Numerics;
using AKG.Core.Parser;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Objects;

/// <summary>
/// Класс модели, содержащей списки всех элементов 
/// </summary>
public class ObjModel
{
    private float _scale;

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
    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Delta = _scale / 10.0f;
        }
    }

    // Смещение модели
    public Vector3 Translation { get; set; } = Vector3.Zero;

    // Вращение модели (углы в радианах по осям X, Y, Z).
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    // Дополнительная величина, например, для шага перемещения
    public float Delta { get; set; }

    public Vector3 GetOptimalTranslationStep()
    {
        float dx = Max.X - Min.X;
        float dy = Max.Y - Min.Y;
        float dz = Max.Z - Min.Z;

        float stepX = dx / 50.0f;
        float stepY = dy / 50.0f;
        float stepZ = dz / 50.0f;

        return new Vector3(stepX, stepY, stepZ);
    }
    
    /// <summary>
    /// Применяет преобразование к вершинам модели после перемножений матриц World x View x Projection x Viewport
    /// </summary>
    /// <param name="camera">Камера, которой смотрят на модель</param>
    /// <param name="finalTransform">Матрица, финального преобразования</param>
    public void ApplyFinalTransformation(Matrix4x4 finalTransform, Camera camera)
    {
        int count = OriginalVertices.Count;
        Parallel.For(0, count, i =>
        {
            var v = Vector4.Transform(OriginalVertices[i], finalTransform);
            if (v.W > camera.ZNear) 
            {
                v /= v.W;
            }
            
            /*if (v.W != 0)
            {
                v /= v.W;
            }*/

            TransformedVertices[i] = v;
        });
    }
}