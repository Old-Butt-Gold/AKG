using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Extensions;
using AKG.Core.Objects;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Renderer;

public static class WireframeRenderer
{
    /// <summary>
    /// Рисует проволочную 3D модель с использованием алгоритма Брезенхэма для растеризации линий.
    /// Рисование производится на WriteableBitmap, которая затем может быть установлена, например, как Source для Image.
    /// </summary>
    /// <param name="model">Объект модели, в котором уже заполнены TransformedVertices</param>
    /// <param name="wb">WriteableBitmap, куда будет производиться отрисовка</param>
    /// <param name="color">Цвет линий (ARGB)</param>
    /// <param name="camera">Камера для проверки диапазона z</param>
    public static void DrawWireframe(ObjModel model, WriteableBitmap wb, Color color, Camera camera, int thickness)
    {
        // Определим цвет в формате BGRA (WriteableBitmap обычно использует PixelFormat Bgra32)
        int intColor = color.ColorToIntBgra();

        wb.Lock();

        unsafe
        {
            // Получаем указатель на начало буфера пикселей
            int* pBackBuffer = (int*)wb.BackBuffer;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;

            Parallel.ForEach(model.Faces, face =>
            {
                int count = face.Vertices.Count;
                if (count < 2)
                    return; // Пропускаем, если грань не имеет хотя бы двух вершин

                for (int i = 0; i < count; i++)
                {
                    // Вычисляем индексы вершин (в OBJ индексы начинаются с 1)
                    int index1 = face.Vertices[i].VertexIndex - 1;
                    int index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

                    // Проверяем корректность индексов
                    if (index1 < 0 || index1 >= model.TransformedVertices.Length ||
                        index2 < 0 || index2 >= model.TransformedVertices.Length)
                        continue;

                    // Вычисляем экранные координаты вершин
                    int x0 = (int)Math.Round(model.TransformedVertices[index1].X);
                    int y0 = (int)Math.Round(model.TransformedVertices[index1].Y);
                    int x1 = (int)Math.Round(model.TransformedVertices[index2].X);
                    int y1 = (int)Math.Round(model.TransformedVertices[index2].Y);
                    float z0 = model.TransformedVertices[index1].Z;
                    float z1 = model.TransformedVertices[index2].Z;

                    // Если обе точки явно вне экрана или вне диапазона z – пропускаем
                    if ((x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) ||
                        (y0 >= height && y1 >= height) || (y0 <= 0 && y1 <= 0) ||
                        (z0 < camera.ZNear || z1 < camera.ZNear) || (z0 > camera.ZFar || z1 > camera.ZFar))
                    {
                        continue;
                    }

                    // Отрисовываем линию с использованием алгоритма Брезенхэма (все записи будут в один и тот же цвет)
                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor, thickness);
                }
            });
        }

        // Сообщаем системе, что изменился весь буфер
        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }
    
    public static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1, int color, int thickness)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
 
        while (true)
        {
            DrawThickPixel(buffer, width, height, x0, y0, color, thickness);
 
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
 
    /// <summary>
    /// Отрисовывает "толстый" пиксель, закрашивая область вокруг него
    /// </summary>
    private static unsafe void DrawThickPixel(int* buffer, int width, int height, int x, int y, int color, int thickness)
    {
        int radius = thickness / 2;  // Определяем радиус заполнения
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                int px = x + i;
                int py = y + j;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    buffer[py * width + px] = color;
                }
            }
        }
    }
    
    public static void DrawLights(Scene scene, WriteableBitmap wb)
    {
        var view = scene.Camera.GetViewMatrix();
        var projection = scene.Camera.GetProjectionMatrix();
        var viewport = scene.GetViewportMatrix();

        unsafe
        {
            int* buffer = (int*)wb.BackBuffer;

            foreach (var light in scene.Lights)
            {
                var screenPosition = light.TransformLightToScreen(view, projection, viewport);

                DrawLight(buffer, wb.PixelWidth, wb.PixelHeight, screenPosition, light.Intensity);
            }
        }
    }
    
    public static unsafe void DrawLight(int* buffer, int width, int height, Vector3 screenPosition, float intensity)
    {
        int x = (int)screenPosition.X;
        int y = (int)screenPosition.Y;

        int lightColor = Colors.Peru.ColorToIntBgra();

        int rayLength = (int)(intensity * 5);

        float[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };

        foreach (var angle in angles)
        {
            var radians = angle * MathF.PI / 180.0f;

            int xEnd = x + (int)(rayLength * MathF.Cos(radians));
            int yEnd = y + (int)(rayLength * MathF.Sin(radians));

            DrawLineBresenham(buffer, width, height, x, y, xEnd, yEnd, lightColor, (int)(intensity * 0.4));
        }

        DrawLineBresenham(buffer, width, height, x, y, x, y, lightColor, (int)(intensity * 4));
    }
    
    
    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        int intColor = clearColor.ColorToIntBgra();

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

    public static void RenderSelectionOutlineDoublePass(Scene scene, WriteableBitmap wb, Color outlineColor)
    {
        if (scene.SelectedModel is null)
            return;
 
        var selected = scene.SelectedModel;
 
        var view = scene.Camera.GetViewMatrix();
        var projection = scene.Camera.GetProjectionMatrix();
        var viewport = scene.GetViewportMatrix();
 
        var outlineWorld = Transformations.CreateWorldTransform(
            selected.Scale * 1.001f,
            Matrix4x4.CreateFromYawPitchRoll(selected.Rotation.Y, selected.Rotation.X, selected.Rotation.Z),
            selected.Translation);
        var finalOutlineTransform = outlineWorld * view * projection * viewport;
 
        selected.ApplyFinalTransformation(finalOutlineTransform, scene.Camera);
 
        DrawWireframe(selected, wb, outlineColor, scene.Camera, 3);
    }
}