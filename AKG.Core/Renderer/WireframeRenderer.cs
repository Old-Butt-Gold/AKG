using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Renderer;

public static class WireframeRenderer
{
    /// <summary>
    /// Рисует проволочную 3D модель с использованием алгоритма Брезенхэма для растеризации линий.
    /// Рисование производится на WriteableBitmap, которая затем может быть установлена, например, как Source для Image.
    /// </summary>
    /// <param name="model">Объект модели с заполненным списком TransformedVertices</param>
    /// <param name="wb">WriteableBitmap, куда будут записаны пиксели</param>
    /// <param name="color">Цвет линий</param>
    public static void DrawWireframe(ObjModel model, WriteableBitmap wb, Color color)
    {
        // Определим цвет в формате BGRA (WriteableBitmap обычно использует PixelFormat Bgra32)
        int intColor = color.ColorToIntBGRA();

        wb.Lock();

        unsafe
        {
            // Получаем указатель на начало буфера пикселей
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;

            // Для каждой грани модели
            foreach (var face in model.Faces)
            {
                int count = face.Vertices.Count;
                if (count < 2)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    // Индексы в файле OBJ начинаются с 1, поэтому вычитаем 1
                    int index1 = face.Vertices[i].VertexIndex - 1;
                    int index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

                    // Проверяем диапазон индексов
                    if (index1 < 0 || index1 >= model.TransformedVertices.Length ||
                        index2 < 0 || index2 >= model.TransformedVertices.Length)
                        continue;

                    // Получаем экранные координаты (используем double для вычислений, затем преобразуем к int)
                    int x0 = (int)Math.Round(model.TransformedVertices[index1].X);
                    int y0 = (int)Math.Round(model.TransformedVertices[index1].Y);
                    int x1 = (int)Math.Round(model.TransformedVertices[index2].X);
                    int y1 = (int)Math.Round(model.TransformedVertices[index2].Y);

                    // Рисуем линию алгоритмом Брезенхэма
                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor);
                }
            }
        }

        // Сообщаем системе, что изменился весь буфер
        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }
    
    /// <summary>
    /// Рисует линию с помощью алгоритма Брезенхэма.
    /// Работает с указателем на BackBuffer WriteableBitmap.
    /// </summary>
    /// <param name="buffer">Указатель на массив пикселей</param>
    /// <param name="width">Ширина изображения (в пикселях)</param>
    /// <param name="height">Высота изображения (в пикселях)</param>
    /// <param name="x0">Начальная координата X</param>
    /// <param name="y0">Начальная координата Y</param>
    /// <param name="x1">Конечная координата X</param>
    /// <param name="y1">Конечная координата Y</param>
    /// <param name="color">Цвет линии в формате ARGB (целое число)</param>
    public static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1, int color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Если координаты внутри экрана, установим пиксель
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                buffer[y0 * width + x0] = color;
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        int intColor = (clearColor.A << 24) | (clearColor.R << 16) | (clearColor.G << 8) | clearColor.B;

        wb.Lock();

        try
        {
            unsafe
            {
                int* pBackBuffer = (int*)wb.BackBuffer;

                for (int i = 0; i < wb.PixelHeight; i++)
                {
                    for (int j = 0; j < wb.PixelWidth; j++)
                    {
                        *pBackBuffer++ = intColor;
                    }
                }
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        }
        finally
        {
            wb.Unlock();
        }
    }

    public static void Draw3DSelectionHighlight(Scene scene, ObjModel model, WriteableBitmap wb, Color highlightColor)
    {
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);
        var view = scene.Camera.GetViewMatrix();
        var projection = scene.Camera.GetProjectionMatrix();
        var viewport = scene.GetViewportMatrix();
        var finalTransform = world * view * projection * viewport;

        // Предполагается, что model.Min и model.Max заданы в объектном (локальном) пространстве
        Vector4[] corners = new Vector4[8];
        corners[0] = new Vector4(model.Min.X, model.Min.Y, model.Min.Z, 1);
        corners[1] = new Vector4(model.Max.X, model.Min.Y, model.Min.Z, 1);
        corners[2] = new Vector4(model.Min.X, model.Max.Y, model.Min.Z, 1);
        corners[3] = new Vector4(model.Max.X, model.Max.Y, model.Min.Z, 1);
        corners[4] = new Vector4(model.Min.X, model.Min.Y, model.Max.Z, 1);
        corners[5] = new Vector4(model.Max.X, model.Min.Y, model.Max.Z, 1);
        corners[6] = new Vector4(model.Min.X, model.Max.Y, model.Max.Z, 1);
        corners[7] = new Vector4(model.Max.X, model.Max.Y, model.Max.Z, 1);

        // Преобразуем каждую вершину в экранное пространство
        Point[] screenCorners = new Point[8];
        for (int i = 0; i < 8; i++)
        {
            Vector4 v = Vector4.Transform(corners[i], finalTransform);
            if (v.W > scene.Camera.ZNear)
            {
                v /= v.W; 
            }

            screenCorners[i] = new Point(v.X, v.Y);
        }

        // Определяем ребра 3D-бокса: 12 ребер (4 нижних, 4 верхних, 4 вертикальных)
        int[][] edges = new int[][]
        {
            [0, 1], [1, 3], [3, 2], [2, 0], // нижняя грань
            [4, 5], [5, 7], [7, 6], [6, 4], // верхняя грань
            [0, 4], [1, 5], [2, 6], [3, 7] // вертикальные ребра
        };

        int intColor = highlightColor.ColorToIntBGRA();
        unsafe
        {
            wb.Lock();
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            foreach (var edge in edges)
            {
                int x0 = (int)Math.Round(screenCorners[edge[0]].X);
                int y0 = (int)Math.Round(screenCorners[edge[0]].Y);
                int x1 = (int)Math.Round(screenCorners[edge[1]].X);
                int y1 = (int)Math.Round(screenCorners[edge[1]].Y);
                DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor);
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, width, height));
            wb.Unlock();
        }
    }
}