using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Extensions;
using AKG.Core.Objects;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Renderer;

public static class Rasterizer
{
    // Z-буфер: хранит глубину для каждого пикселя; 
    // массив организован как [x, y] (строка, столбец)
    private static float[,]? _zBuffer;
    
    /// <summary>
    /// Инициализирует Z-буфер заданного размера, заполняя его значениями, равными камере.ZFar.
    /// </summary>
    public static void ClearZBuffer(int width, int height, Camera camera)
    {
        _zBuffer ??= new float[width, height];
        float initDepth = camera.ZFar;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) 
                _zBuffer[x, y] = initDepth;
    }
    
    /// <summary>
    /// Растеризует (заполняет) треугольники для каждой грани модели.
    /// Для каждой грани, состоящей из 3+ вершин, применяется фан‑трайангуляция.
    /// Для каждой треугольной части производится backface culling (с использованием нормали)
    /// и рассчитывается интенсивность освещения по модели Ламберта.
    /// Затем вызывается метод, который заполняет треугольник с использованием Z-буфера.
    /// </summary>
    public static unsafe void DrawFilledTriangle(ObjModel model, WriteableBitmap wb, Color color, Camera camera)
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
                if (Vector3.Dot(normal, viewDirection) > 0) continue; // Если скалярное произведение положительное, грань отвернута
                
                // Расчет интенсивности освещения по модели Ламберта
                var shadedColor = color.ApplyLambert(normal, camera.LambertLight);

                // Получаем экранные координаты (после всех преобразований)
                Vector3 screenV0 = model.TransformedVertices[idx0].AsVector3();
                Vector3 screenV1 = model.TransformedVertices[idx1].AsVector3();
                Vector3 screenV2 = model.TransformedVertices[idx2].AsVector3();

                // Растеризуем треугольник с заливкой и Z-тестом
                DrawFilledTriangle(screenV0, screenV1, screenV2, shadedColor, buffer, width, height);
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
    private static unsafe void DrawFilledTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Color color, int* buffer, int width, int height)
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
                float beta  = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) * invDenom;
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
                        buffer[y * width + x] = color.ColorToIntBGRA();
                    }
                }
            }
        }
    }
}