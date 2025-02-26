using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Extensions;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Renderer;

public static class Rasterizer
{
    // Z-буфер: хранит глубину для каждого пикселя; 
    private static float[,]? _zBuffer;

    public static void ClearZBuffer(int width, int height, Camera camera)
    {
        _zBuffer ??= new float[width, height];
        float initDepth = camera.ZFar;
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            _zBuffer[x, y] = initDepth;
    }

    #region Lambert

    /// <summary>
    /// Растеризует (заполняет) треугольники для каждой грани модели.
    /// Для каждой грани, состоящей из 3+ вершин, применяется фан‑трайангуляция.
    /// Для каждой треугольной части производится backface culling (с использованием нормали)
    /// и рассчитывается интенсивность освещения по модели Ламберта.
    /// Затем вызывается метод, который заполняет треугольник с использованием Z-буфера.
    /// </summary>
    public static unsafe void DrawFilledTriangleLambert(ObjModel model, WriteableBitmap wb, Color color, Camera camera,
        List<Light> lights)
    {
        int width = wb.PixelWidth;
        int height = wb.PixelHeight;

        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        wb.Lock();

        int* buffer = (int*)wb.BackBuffer;

        // Для каждой грани модели
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            //Если грань содержит больше 3 вершин, выполняем трайангуляцию
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                //Вычисляем нормаль треугольника в мировых координатах
                Vector3 worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                Vector3 worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                Vector3 worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                Vector3 edge1 = worldV1 - worldV0;
                Vector3 edge2 = worldV2 - worldV0;

                // Эту нормаль бы сохранять где-то на будущее
                Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                Vector3 viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(normal, viewDirection) > 0)
                    continue; // Если скалярное произведение положительное, грань отвернута

                // Расчет интенсивности освещения по модели Ламберта
                var shadedColor = color.ApplyLambert(normal, lights);

                // Получаем экранные координаты (после всех преобразований)
                Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
                Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
                Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width)
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0)
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height)
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || (screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear)
                    || (screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar))
                {
                    continue;
                }

                // Растеризуем треугольник с заливкой и Z-тестом
                DrawFilledTriangleLambert(screenV0, screenV1, screenV2, shadedColor, buffer, width, height);
            }
        });

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    /// <summary>
    /// Растеризует (заполняет) один треугольник, заданный тремя вершинами в экранном пространстве.
    /// Метод использует сканирующую линию с вычислением барицентрических координат для интерполяции глубины.
    /// Отбраковка невидимых фрагментов осуществляется с помощью Z-буфера.
    /// </summary>
    private static unsafe void DrawFilledTriangleLambert(Vector3 v0, Vector3 v1, Vector3 v2, Color color, int* buffer,
        int width, int height)
    {
        // Определяем ограничивающий прямоугольник (обрамлены Math.Max и Math.Min, чтобы не уходили за экран)
        int minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Вычисляем знаменатель барицентрических координат
        float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denom) < float.Epsilon) return; // Вырожденный треугольник

        float invDenom = 1.0f / denom;

        for (var y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= height)
                return;

            for (var x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= width)
                    continue;

                // Вычисляем барицентрические координаты: alpha, beta, gamma
                float alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenom;
                float beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenom;
                float gamma = 1 - alpha - beta;

                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину по барицентрическим координатам
                    float depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Если новый фрагмент ближе (меньшее значение depth) – обновляем Z-буфер и рисуем пиксель
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;
                        buffer[y * width + x] = color.ColorToIntBgra();
                    }
                }
            }
        }
    }

    #endregion


    // Поддерживаются два режима:
    // FilledTrianglesPhong – вычисление цвета на уровне пикселя (обычное Фонговое затенение)
    // FilledTrianglesAverageFaceNormalPhong – использование усреднённых нормалей вершин (Гуравское затенение)

    #region FilledTrianglesPhong

    /// <summary>
    /// Растеризует треугольники для каждой грани модели с применением фан-трайангуляции, backface culling и модели Фонга.
    /// Для каждой треугольной части вычисляются экранные координаты и, если треугольник видим (с учетом нормали),
    /// происходит заполнение с использованием Z-буфера и вычислением цвета по модели Фонга.
    /// </summary>
    public static unsafe void DrawFilledTrianglePhong(ObjModel model, WriteableBitmap wb,
        Camera camera, List<Light> lights)
    {
        int width = wb.PixelWidth;
        int height = wb.PixelHeight;

        // Вычисляем мировую матрицу на основе масштабирования, вращения и трансляции модели
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        wb.Lock();
        int* buffer = (int*)wb.BackBuffer;

        // Для каждой грани модели (фан-трайангуляция)
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            // Для каждой треугольной части грани
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                // Вычисляем мировые координаты вершин (для backface culling)
                Vector3 worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                Vector3 worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                Vector3 worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                // Вычисляем нормаль треугольника (в мировых координатах)
                Vector3 edge1 = worldV1 - worldV0;
                Vector3 edge2 = worldV2 - worldV0;
                Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                Vector3 viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(faceNormal, viewDirection) > 0)
                    continue; // Если скалярное произведение положительное, грань отвернута

                // Получаем экранные координаты (уже после всех преобразований)
                Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
                Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
                Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width)
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0)
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height)
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || (screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear)
                    || (screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar))
                {
                    continue;
                }

                // Определяем нормали для затенения:
                // Если в модели заданы нормали для вершин, используем их; иначе – используем нормаль грани.
                var n0 = (face.Vertices[0].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[0].NormalIndex - 1], world)
                    : faceNormal;
                var n1 = (face.Vertices[j].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j].NormalIndex - 1], world)
                    : faceNormal;
                var n2 = (face.Vertices[j + 1].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world)
                    : faceNormal;

                /* Было
                 var n0 = (face.Vertices[0].NormalIndex > 0)
                    ? Vector4.Transform(model.Normals[face.Vertices[0].NormalIndex - 1], world).AsVector3()
                    : faceNormal;
                var n1 = (face.Vertices[j].NormalIndex > 0)
                    ? Vector4.Transform(model.Normals[face.Vertices[j].NormalIndex - 1], world).AsVector3()
                    : faceNormal;
                var n2 = (face.Vertices[j + 1].NormalIndex > 0)
                    ? Vector4.Transform(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world).AsVector3()
                    : faceNormal;
                 */

                // Отрисовываем треугольник с Фонговым затенением.
                DrawFilledTrianglePhong(screenV0, screenV1, screenV2,
                    n0, n1, n2, worldV0, worldV1, worldV2,
                    buffer, width, height, lights, camera);
            }
        });

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    #endregion

    #region FilledTrianglesAverageFaceNormalPhong

    /// <summary>
    /// Растеризует треугольники для каждой грани модели с применением фан-трайангуляции, backface culling и модели Фонга.
    /// Для каждой треугольной части вычисляются экранные координаты и, если треугольник видим (с учетом нормали),
    /// происходит заполнение с использованием Z-буфера и вычислением цвета по модели Фонга.
    /// </summary>
    public static unsafe void FilledTrianglesAverageFaceNormalPhong(ObjModel model, WriteableBitmap wb,
        Camera camera, List<Light> lights)
    {
        int width = wb.PixelWidth;
        int height = wb.PixelHeight;

        // Вычисляем мировую матрицу на основе масштабирования, вращения и трансляции модели
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        wb.Lock();
        int* buffer = (int*)wb.BackBuffer;

        model.CalculateVertexNormals(world);

        // Для каждой грани модели (фан-трайангуляция)
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            // Для каждой треугольной части грани
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                // Вычисляем мировые координаты вершин (для backface culling)
                Vector3 worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                Vector3 worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                Vector3 worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                // Вычисляем нормаль треугольника (в мировых координатах)
                Vector3 edge1 = worldV1 - worldV0;
                Vector3 edge2 = worldV2 - worldV0;
                Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Backface culling: если треугольник обращён от камеры, отбраковываем грань
                Vector3 viewDirection = worldV0 - camera.Eye; // Вектор взгляда от камеры к вершине
                if (Vector3.Dot(faceNormal, viewDirection) > 0)
                    continue; // Если скалярное произведение положительное, грань отвернута

                // Получаем экранные координаты (уже после всех преобразований)
                Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
                Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
                Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();

                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width)
                    || (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0)
                    || (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height)
                    || (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0)
                    || (screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear)
                    || (screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar))
                {
                    continue;
                }

                var n0 = model.VertexNormals[idx0];
                var n1 = model.VertexNormals[idx1];
                var n2 = model.VertexNormals[idx2];

                DrawFilledTrianglePhong(screenV0, screenV1, screenV2,
                    n0, n1, n2, worldV0, worldV1, worldV2,
                    buffer, width, height, lights, camera);
            }
        });

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    #endregion

    /// <summary>
    /// Растеризует один треугольник с Фонговым затенением.
    /// Для каждого пикселя внутри ограничивающего прямоугольника вычисляются барицентрические координаты,
    /// интерполируется глубина, а также мировая позиция и нормаль, после чего вычисляется итоговый цвет фрагмента по модели Фонга.
    /// Отбраковка невидимых фрагментов производится с помощью Z-буфера.
    /// </summary>
    private static unsafe void DrawFilledTrianglePhong(Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 w0, Vector3 w1, Vector3 w2,
        int* buffer, int width, int height, List<Light> lights, Camera camera)
    {
        // Ограничивающий прямоугольник (не выходит за пределы экрана)
        int minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Вычисляем знаменатель барицентрических координат
        float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denom) < float.Epsilon) return; // Вырожденный треугольник
        float invDenom = 1.0f / denom;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Вычисляем барицентрические координаты: alpha, beta, gamma
                float alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenom;
                float beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenom;
                float gamma = 1 - alpha - beta;

                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину
                    float depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Z-тест: если новый фрагмент ближе, обновляем Z-буфер и цвет пикселя
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;

                        // Интерполируем нормаль: линейная интерполяция нормалей вершин
                        var interpNormal = Vector3.Normalize(alpha * n0 + beta * n1 + gamma * n2);

                        // Интерполируем мировую позицию фрагмента (для расчёта вектора взгляда)
                        var fragWorld = alpha * w0 + beta * w1 + gamma * w2;

                        // Вектор от фрагмента к камере.
                        // Нормализация нужна для расчета зеркальной составляющей.
                        var viewDirection = Vector3.Normalize(camera.Eye - fragWorld);

                        buffer[y * width + x] = Light.ApplyPhongShading(lights, interpNormal, viewDirection, fragWorld)
                            .ColorToIntBgra();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Объединённый метод, который для каждой грани модели (с фан‑трайангуляцией)
    /// вычисляет необходимые параметры и затем для каждого треугольника выполняет
    /// наложение текстур: диффузной карты, карты нормалей и зеркальной карты.
    /// </summary>
    /// <param name="model">Модель (объект ObjModel)</param>
    /// <param name="wb">WriteableBitmap для отрисовки</param>
    /// <param name="camera">Камера сцены</param>
    /// <param name="lights">Список источников света</param>
    public static unsafe void DrawTexturedTriangles(ObjModel model, WriteableBitmap wb, Camera camera,
        List<Light> lights)
    {
        int width = wb.PixelWidth;
        int height = wb.PixelHeight;

        // 1. Вычисляем мировую матрицу для модели
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        wb.Lock();
        int* buffer = (int*)wb.BackBuffer;

        // 2. Проходим по каждой грани модели (с фан‑трайангуляцией)
        Parallel.ForEach(model.Faces, face =>
        {
            if (face.Vertices.Count < 3) return;

            // Фан‑трайангуляция: для каждой грани разбиваем её на треугольники,
            // используя первую вершину и пары последовательных вершин
            for (int j = 1; j < face.Vertices.Count - 1; j++)
            {
                int idx0 = face.Vertices[0].VertexIndex - 1;
                int idx1 = face.Vertices[j].VertexIndex - 1;
                int idx2 = face.Vertices[j + 1].VertexIndex - 1;

                if (idx0 < 0 || idx1 < 0 || idx2 < 0 ||
                    idx0 >= model.TransformedVertices.Length ||
                    idx1 >= model.TransformedVertices.Length ||
                    idx2 >= model.TransformedVertices.Length)
                    continue;

                // 3. Вычисляем мировые координаты вершин
                Vector3 worldV0 = Vector4.Transform(model.OriginalVertices[idx0], world).AsVector3();
                Vector3 worldV1 = Vector4.Transform(model.OriginalVertices[idx1], world).AsVector3();
                Vector3 worldV2 = Vector4.Transform(model.OriginalVertices[idx2], world).AsVector3();

                // 4. Вычисляем нормаль треугольника для backface culling
                Vector3 edge1 = worldV1 - worldV0;
                Vector3 edge2 = worldV2 - worldV0;
                Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                // Если треугольник обращён от камеры, пропускаем его
                Vector3 viewDir = worldV0 - camera.Eye;
                if (Vector3.Dot(faceNormal, viewDir) > 0)
                    continue;

                // 5. Получаем экранные координаты вершин
                Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
                Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
                Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();

                // Если треугольник полностью вне экрана – пропускаем
                if ((screenV0.X >= width && screenV1.X >= width && screenV2.X >= width) ||
                    (screenV0.X <= 0 && screenV1.X <= 0 && screenV2.X <= 0) ||
                    (screenV0.Y >= height && screenV1.Y >= height && screenV2.Y >= height) ||
                    (screenV0.Y <= 0 && screenV1.Y <= 0 && screenV2.Y <= 0) ||
                    (screenV0.Z < camera.ZNear || screenV1.Z < camera.ZNear || screenV2.Z < camera.ZNear) ||
                    (screenV0.Z > camera.ZFar || screenV1.Z > camera.ZFar || screenV2.Z > camera.ZFar))
                {
                    continue;
                }

                // 6. Извлекаем UV-координаты для каждой вершины
                var uv0 = model.TextureCoords[face.Vertices[0].TextureIndex - 1]; // разделить на W еще 
                var uv1 = model.TextureCoords[face.Vertices[j].TextureIndex - 1];
                var uv2 = model.TextureCoords[face.Vertices[j + 1].TextureIndex - 1];

                // 7. Определяем нормали для затенения (используем нормали вершин, если заданы)
                var n0 = (face.Vertices[0].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[0].NormalIndex - 1], world)
                    : faceNormal;
                var n1 = (face.Vertices[j].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j].NormalIndex - 1], world)
                    : faceNormal;
                var n2 = (face.Vertices[j + 1].NormalIndex > 0)
                    ? Vector3.TransformNormal(model.Normals[face.Vertices[j + 1].NormalIndex - 1], world)
                    : faceNormal;

                // 8. Мировые координаты фрагментов (для расчёта вектора взгляда)
                var w0 = worldV0;
                var w1 = worldV1;
                var w2 = worldV2;

                // 9. Загружаем текстуры из материала (если материал задан)
                Material? material = null;
                BitmapImage? diffuseTex = null;
                BitmapImage? normalTex = null;
                BitmapImage? specularTex = null;
                if (model.Materials != null && !string.IsNullOrEmpty(face.MaterialName))
                {
                    model.Materials.TryGetValue(face.MaterialName, out material);
                }

                if (material != null)
                {
                    if (!string.IsNullOrEmpty(material.DiffuseMap))
                        diffuseTex = TextureLoader.Load(material.DiffuseMap);
                    if (!string.IsNullOrEmpty(material.NormalMap))
                        normalTex = TextureLoader.Load(material.NormalMap);
                    if (!string.IsNullOrEmpty(material.SpecularMap))
                        specularTex = TextureLoader.Load(material.SpecularMap);
                }

                // 10. Вызываем функцию отрисовки треугольника с наложением текстур
                DrawFilledTriangleTexture(screenV0, screenV1, screenV2, n0, n1, n2, w0, w1, w2, uv0, uv1, uv2,
                    buffer, width, height, lights, camera, diffuseTex, normalTex, specularTex, model);
            }

        });

        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    /// <summary>
    /// Метод, который для одного треугольника интерполирует параметры для каждого пикселя
    /// и рассчитывает итоговый цвет с учетом наложения диффузной карты, карты нормалей и зеркальной карты.
    /// </summary>
    private static unsafe void DrawFilledTriangleTexture(Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 n0, Vector3 n1, Vector3 n2, Vector3 w0, Vector3 w1, Vector3 w2,
        Vector3 uv0, Vector3 uv1, Vector3 uv2,
        int* buffer, int width, int height, List<Light> lights, Camera camera,
        BitmapImage? diffuseTex, BitmapImage? normalTex, BitmapImage? specularTex, ObjModel model)
    {
        // Ограничивающий прямоугольник (не выходит за пределы экрана)
        int minX = Math.Max(0, (int)Math.Floor(Math.Min(v0.X, Math.Min(v1.X, v2.X))));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(v0.X, Math.Max(v1.X, v2.X))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(v0.Y, Math.Min(v1.Y, v2.Y))));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(v0.Y, Math.Max(v1.Y, v2.Y))));

        // Вычисляем знаменатель барицентрических координат
        float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
        if (Math.Abs(denom) < float.Epsilon) return; // Вырожденный треугольник
        float invDenom = 1.0f / denom;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Вычисляем барицентрические координаты: alpha, beta, gamma
                float alpha = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) * invDenom;
                float beta = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenom;
                float gamma = 1 - alpha - beta;

                // Если точка внутри треугольника (включая границы)
                if (alpha >= 0 && beta >= 0 && gamma >= 0)
                {
                    // Интерполируем глубину
                    float depth = alpha * v0.Z + beta * v1.Z + gamma * v2.Z;
                    // Z-тест: если новый фрагмент ближе, обновляем Z-буфер и цвет пикселя
                    if (depth < _zBuffer![x, y])
                    {
                        _zBuffer[x, y] = depth;

                        // Интерполируем UV-координаты
                        Vector3 uv = alpha * uv0 + beta * uv1 + gamma * uv2;
                        // Интерполируем мировую позицию фрагмента
                        Vector3 fragWorld = alpha * w0 + beta * w1 + gamma * w2;
                        // Интерполируем нормаль фрагмента
                        Vector3 interpNormal = Vector3.Normalize(alpha * n0 + beta * n1 + gamma * n2);

                        // Если задана карта нормалей, заменяем интерполированную нормаль
                        if (normalTex != null)
                        {
                            Color normColor = TextureSampler.Sample(normalTex, uv.X, uv.Y);
                            var mapNormal = new Vector3(
                                (normColor.R / 255f) * 2f - 1f,
                                (normColor.G / 255f) * 2f - 1f,
                                (normColor.B / 255f) * 2f - 1f);
                            mapNormal = Vector3.Normalize(mapNormal);

                            // Применяем вращение модели к нормали (если требуется)
                            var rotation = Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X,
                                model.Rotation.Z);
                            mapNormal = Vector3.TransformNormal(mapNormal, rotation);
                            interpNormal = mapNormal;
                        }

                        // Вычисляем вектор взгляда (от фрагмента к камере)
                        // Нормализация нужна для расчета зеркальной составляющей.
                        var viewDir = Vector3.Normalize(camera.Eye - fragWorld);

                        // Базовый цвет по модели Фонга (с учетом амбиентной, диффузной и спекулярной составляющих)
                        var ambient = Vector3.Zero;
                        var diffuse = Vector3.Zero;
                        var specular = Vector3.Zero;
                        var baseDiffuse = lights[0].Diffuse.ToVector3(); // по умолчанию
                        var baseAmbient = lights[0].Ambient.ToVector3();
                        float ks = lights[0].Ks;

                        if (diffuseTex != null)
                        {
                            var diffColor = TextureSampler.Sample(diffuseTex, uv.X, uv.Y);
                            baseDiffuse = diffColor.ToVector3();
                            baseAmbient = baseDiffuse;
                        }

                        if (specularTex != null)
                        {
                            Color specColor = TextureSampler.Sample(specularTex, uv.X, uv.Y);
                            ks = specColor.R / 255f;
                        }


                        foreach (var light in lights)
                        {
                            // Амбиент
                            ambient += baseAmbient * light.Ka;
                            // Диффузная
                            Vector3 lightDir = Vector3.Normalize(light.Direction - fragWorld);
                            float NdotL = MathF.Max(Vector3.Dot(interpNormal, lightDir), 0);
                            diffuse += baseDiffuse * NdotL * light.Kd;
                            // Спекулярная
                            Vector3 reflection = Vector3.Reflect(-lightDir, interpNormal);
                            float RdotV = MathF.Max(Vector3.Dot(reflection, viewDir), 0);
                            if (RdotV > 0)
                            {
                                specular += light.Specular.ToVector3() * ks * MathF.Pow(RdotV, light.Shininess);
                            }
                        }

                        var phong = Vector3.Clamp(ambient + diffuse + specular, Vector3.Zero,
                            new Vector3(255, 255, 255));
                        var finalColor = Color.FromArgb(255, (byte)phong.X, (byte)phong.Y, (byte)phong.Z);
                        buffer[y * width + x] = finalColor.ColorToIntBgra();
                    }
                }
            }
        }
    }
}