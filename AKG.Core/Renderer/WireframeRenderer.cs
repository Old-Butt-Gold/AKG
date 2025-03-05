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
    ///     Рисует проволочную 3D модель с использованием алгоритма Брезенхэма для растеризации линий.
    ///     Рисование производится на WriteableBitmap, которая затем может быть установлена, например, как Source для Image.
    /// </summary>
    /// <param name="model">Объект модели, в котором уже заполнены TransformedVertices</param>
    /// <param name="wb">WriteableBitmap, куда будет производиться отрисовка</param>
    /// <param name="color">Цвет линий (ARGB)</param>
    /// <param name="camera">Камера для проверки диапазона z</param>
    public static void DrawWireframe(ObjModel model, WriteableBitmap wb, Color color, Camera camera, int thickness)
    {
        // Определим цвет в формате BGRA (WriteableBitmap обычно использует PixelFormat Bgra32)
        var intColor = color.ColorToIntBgra();

        wb.Lock();

        unsafe
        {
            // Получаем указатель на начало буфера пикселей
            var pBackBuffer = (int*)wb.BackBuffer;
            var width = wb.PixelWidth;
            var height = wb.PixelHeight;

            Parallel.ForEach(model.Faces, face =>
            {
                var count = face.Vertices.Count;
                if (count < 2)
                    return; // Пропускаем, если грань не имеет хотя бы двух вершин

                for (var i = 0; i < count; i++)
                {
                    // Вычисляем индексы вершин (в OBJ индексы начинаются с 1)
                    var index1 = face.Vertices[i].VertexIndex - 1;
                    var index2 = face.Vertices[(i + 1) % count].VertexIndex - 1;

                    // Проверяем корректность индексов
                    if (index1 < 0 || index1 >= model.TransformedVertices.Length ||
                        index2 < 0 || index2 >= model.TransformedVertices.Length)
                        continue;

                    // Вычисляем экранные координаты вершин
                    var x0 = (int)Math.Round(model.TransformedVertices[index1].X);
                    var y0 = (int)Math.Round(model.TransformedVertices[index1].Y);
                    var x1 = (int)Math.Round(model.TransformedVertices[index2].X);
                    var y1 = (int)Math.Round(model.TransformedVertices[index2].Y);
                    var z0 = model.TransformedVertices[index1].Z;
                    var z1 = model.TransformedVertices[index2].Z;

                    // Если обе точки явно вне экрана или вне диапазона z – пропускаем
                    if ((x0 >= width && x1 >= width) || (x0 <= 0 && x1 <= 0) ||
                        (y0 >= height && y1 >= height) || (y0 <= 0 && y1 <= 0) ||
                        z0 < camera.ZNear || z1 < camera.ZNear || z0 > camera.ZFar || z1 > camera.ZFar)
                        continue;

                    // Отрисовываем линию с использованием алгоритма Брезенхэма (все записи будут в один и тот же цвет)
                    DrawLineBresenham(pBackBuffer, width, height, x0, y0, x1, y1, intColor, thickness);
                }
            });
        }

        // Сообщаем системе, что изменился весь буфер
        wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }

    public static unsafe void DrawLineBresenham(int* buffer, int width, int height, int x0, int y0, int x1, int y1,
        int color, int thickness)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            DrawThickPixel(buffer, width, height, x0, y0, color, thickness);

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = 2 * err;
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
    ///     Отрисовывает "толстый" пиксель, закрашивая область вокруг него
    /// </summary>
    private static unsafe void DrawThickPixel(int* buffer, int width, int height, int x, int y, int color,
        int thickness)
    {
        var radius = thickness / 2; // Определяем радиус заполнения
        for (var i = -radius; i <= radius; i++)
        for (var j = -radius; j <= radius; j++)
        {
            var px = x + i;
            var py = y + j;
            if (px >= 0 && px < width && py >= 0 && py < height) buffer[py * width + px] = color;
        }
    }

    public static void DrawLights(Scene scene, WriteableBitmap wb)
    {
        var view = scene.Camera.GetViewMatrix();
        var projection = scene.Camera.GetProjectionMatrix();
        var viewport = scene.GetViewportMatrix();

        unsafe
        {
            var buffer = (int*)wb.BackBuffer;

            foreach (var light in scene.Lights)
            {
                var screenPosition = light.TransformLightToScreen(view, projection, viewport);

                DrawLight(buffer, wb.PixelWidth, wb.PixelHeight, screenPosition, light.Intensity);
            }
        }
    }

    public static unsafe void DrawLight(int* buffer, int width, int height, Vector3 screenPosition, float intensity)
    {
        var x = (int)screenPosition.X;
        var y = (int)screenPosition.Y;

        var lightColor = Colors.Peru.ColorToIntBgra();

        var rayLength = 5;

        float[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };

        foreach (var angle in angles)
        {
            var radians = angle * MathF.PI / 180.0f;

            var xEnd = x + (int)(rayLength * MathF.Cos(radians));
            var yEnd = y + (int)(rayLength * MathF.Sin(radians));

            DrawLineBresenham(buffer, width, height, x, y, xEnd, yEnd, lightColor, 1);
        }

        DrawLineBresenham(buffer, width, height, x, y, x, y, lightColor, 4);
        // Отрисовываем центральную точку (опционально)
        DrawLineBresenham(buffer, width, height, x, y, x, y, lightColor, 3);

        // Отрисовываем круг вокруг центральной точки
        var radius = 15; // Радиус круга зависит от интенсивности
        DrawCircleBresenham(buffer, width, height, x, y, radius, lightColor);
    }

    public static unsafe void DrawCircleBresenham(int* buffer, int width, int height, int xc, int yc, int radius,
        int color)
    {
        var x = 0;
        var y = radius;
        var d = 3 - 2 * radius;

        while (x <= y)
        {
            // Отрисовываем 8 точек окружности (симметрия)
            DrawCirclePoints(buffer, width, height, xc, yc, x, y, color);

            if (d < 0)
            {
                d = d + 4 * x + 6;
            }
            else
            {
                d = d + 4 * (x - y) + 10;
                y--;
            }

            x++;
        }
    }

    private static unsafe void DrawCirclePoints(int* buffer, int width, int height, int xc, int yc, int x, int y,
        int color)
    {
        // Отрисовываем 8 симметричных точек окружности
        DrawPixel(buffer, width, height, xc + x, yc + y, color);
        DrawPixel(buffer, width, height, xc - x, yc + y, color);
        DrawPixel(buffer, width, height, xc + x, yc - y, color);
        DrawPixel(buffer, width, height, xc - x, yc - y, color);
        DrawPixel(buffer, width, height, xc + y, yc + x, color);
        DrawPixel(buffer, width, height, xc - y, yc + x, color);
        DrawPixel(buffer, width, height, xc + y, yc - x, color);
        DrawPixel(buffer, width, height, xc - y, yc - x, color);
    }

    private static unsafe void DrawPixel(int* buffer, int width, int height, int x, int y, int color)
    {
        // Проверяем, чтобы координаты были в пределах экрана
        if (x >= 0 && x < width && y >= 0 && y < height) buffer[y * width + x] = color;
    }


    public static void ClearBitmap(WriteableBitmap wb, Color clearColor)
    {
        var intColor = clearColor.ColorToIntBgra();

        wb.Lock();

        try
        {
            unsafe
            {
                var pBackBuffer = (int*)wb.BackBuffer;

                for (var i = 0; i < wb.PixelHeight; i++)
                for (var j = 0; j < wb.PixelWidth; j++)
                    *pBackBuffer++ = intColor;
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
    
  public static void DrawGrid(Scene scene, WriteableBitmap wb, Color color, int gridSize, int step, int thickness = 1)
{
    var camera = scene.Camera;
    var view = camera.GetViewMatrix();
    var projection = camera.GetProjectionMatrix();
    var viewport = scene.GetViewportMatrix();
    var intColor = color.ColorToIntBgra();

    wb.Lock();

    unsafe
    {
        var pBackBuffer = (int*)wb.BackBuffer;
        var width = wb.PixelWidth;
        var height = wb.PixelHeight;

        for (int i = -gridSize; i <= gridSize; i++)
        {
            float worldX = i * step;
            float worldZ = i * step;

            // Линии вдоль оси Z (вертикальные)
            var lineStartX = new Vector3(worldX, 0, -gridSize * step);
            var lineEndX = new Vector3(worldX, 0, gridSize * step);
            var screenStartX = TransformWorldToScreen(lineStartX, view, projection, viewport, camera);
            var screenEndX = TransformWorldToScreen(lineEndX, view, projection, viewport, camera);

            DrawLineIfVisible(pBackBuffer, width, height, screenStartX, screenEndX, intColor, thickness);

            // Линии вдоль оси X (горизонтальные)
            var lineStartZ = new Vector3(-gridSize * step, 0, worldZ);
            var lineEndZ = new Vector3(gridSize * step, 0, worldZ);
            var screenStartZ = TransformWorldToScreen(lineStartZ, view, projection, viewport, camera);
            var screenEndZ = TransformWorldToScreen(lineEndZ, view, projection, viewport, camera);

            DrawLineIfVisible(pBackBuffer, width, height, screenStartZ, screenEndZ, intColor, thickness);
        }
    }

    wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
    wb.Unlock();
}

private static Vector3 TransformWorldToScreen(Vector3 worldPos, Matrix4x4 view, Matrix4x4 projection, Matrix4x4 viewport, Camera camera)
{
    var worldVec = new Vector4(worldPos, 1.0f);
    
    // Преобразование в пространство камеры (view space)
    var viewPos = Vector4.Transform(worldVec, view);
    
    // Проверка на диапазон znear/zfar в view space
    // (Z в view space должен быть между -ZFar и -ZNear)
    if (viewPos.Z < -camera.ZFar || viewPos.Z > -camera.ZNear)
        return new Vector3(-1, -1, -1); // Недопустимые координаты
    
    // Применение проекции
    var clipPos = Vector4.Transform(viewPos, projection);
    
    if (clipPos.W == 0)
        return new Vector3(-1, -1, -1);
    
    // Перспективное деление
    var ndcPos = clipPos / clipPos.W;
    
    // Преобразование в экранные координаты
    var viewportPos = Vector4.Transform(ndcPos, viewport);
    
    return new Vector3(viewportPos.X, viewportPos.Y, viewportPos.Z);
}

private static unsafe void DrawLineIfVisible(int* buffer, int width, int height, Vector3 start, Vector3 end, int color, int thickness)
{
    // Если хотя бы одна точка не прошла проверку видимости (Z < 0)
    if (start.Z < 0 || end.Z < 0)
        return;

    int x0 = (int)Math.Round(start.X);
    int y0 = (int)Math.Round(start.Y);
    int x1 = (int)Math.Round(end.X);
    int y1 = (int)Math.Round(end.Y);

    DrawLineBresenham(buffer, width, height, x0, y0, x1, y1, color, thickness);
}

}