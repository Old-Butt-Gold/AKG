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

    // Счетчики количества граней, использующих каждую вершину.
    public int[] Counters { get; set; } = [];

    // Нормали вершин (рассчитываются путем усреднения нормалей граней).
    public Vector3[] VertexNormals { get; set; } = [];

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
    
    /// <summary>
    /// Рассчитывает нормали вершин на основе нормалей граней.
    /// </summary>
    public void CalculateVertexNormals()
    {
        var world = Transformations.CreateWorldTransform(
            Scale,
            Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z),
            Translation);
        
        // Инициализируем нормали и счетчики нулями
        for (int i = 0; i < OriginalVertices.Count; i++)
        {
            VertexNormals[i] = Vector3.Zero;
            Counters[i] = 0;
        }
        
        // Для каждой грани выполняем фан-трайангуляцию
        Parallel.ForEach(Faces, face =>
        {
            if (face.Vertices.Count < 3)
                return;
        
            // Фан-трайангуляция: используем первую вершину как базовую и формируем треугольники
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= OriginalVertices.Count || idx1 >= OriginalVertices.Count || idx2 >= OriginalVertices.Count)
                    continue;

                // Преобразуем исходные вершины (без перспективного деления)
                var worldV0 = Vector4.Transform(OriginalVertices[idx0], world).AsVector3();
                var worldV1 = Vector4.Transform(OriginalVertices[idx1], world).AsVector3();
                var worldV2 = Vector4.Transform(OriginalVertices[idx2], world).AsVector3();

                // Вычисляем нормаль данного треугольника (важен порядок вершин)
                var edge1 = worldV1 - worldV0;
                var edge2 = worldV2 - worldV0;
                var triNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Добавляем нормаль треугольника к каждой из вершин
                AddFaceNormalToVertex(idx0, triNormal);
                AddFaceNormalToVertex(idx1, triNormal);
                AddFaceNormalToVertex(idx2, triNormal);
            }
        });

        // Усредняем нормали для каждой вершины
        Parallel.For(0, VertexNormals.Length, i =>
        {
            if (Counters[i] > 0)
            {
                VertexNormals[i] = Vector3.Normalize(VertexNormals[i] / Counters[i]);
            }
        });

        void AddFaceNormalToVertex(int idx, Vector3 normal)
        {
            VertexNormals[idx] += normal;
            Counters[idx]++;
        }
    }
    
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
            if (v.W > camera.ZNear && v.W < camera.ZFar) 
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