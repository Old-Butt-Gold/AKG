using System.Numerics;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;

namespace AKG.Core.Renderer;

public static class BackgroundRenderer
{
    public static void RenderBackground(Scene scene, WriteableBitmap wb)
    {
        if (scene.Background == null) return;

        int width = wb.PixelWidth;
        int height = wb.PixelHeight;
        var camera = scene.Camera;

        // Вычисляем обратные матрицы единожды
        Matrix4x4.Invert(camera.GetProjectionMatrix(), out var invProj);
        Matrix4x4.Invert(camera.GetViewMatrix(), out var invView);

        wb.Lock();
        unsafe
        {
            byte* buffer = (byte*)wb.BackBuffer;
            int stride = wb.BackBufferStride;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float ndcX = (2f * x / width) - 1f;
                    float ndcY = 1f - (2f * y / height);
                    var rayClip = new Vector4(ndcX, ndcY, 1f, 1f);
                
                    // Преобразуем из клип-пространства в мировые координаты,
                    // используя предвычисленные обратные матрицы
                    var rayEye = Vector4.Transform(rayClip, invProj);
                    var rayWorld4 = Vector4.Transform(rayEye, invView);
                    var rayWorld = Vector3.Normalize(new Vector3(rayWorld4.X, rayWorld4.Y, rayWorld4.Z));

                    // Получаем цвет из HDRi
                    var color = scene.Background.SampleSpherical(rayWorld);
                    
                    int offset = y * stride + x * 4;
                    
                    buffer[offset] = (byte)color.Z; // B
                    buffer[offset + 1] = (byte)color.Y; // G
                    buffer[offset + 2] = (byte)color.X; // R
                    buffer[offset + 3] = 255; // A
                }
            });
        }
        wb.AddDirtyRect(new (0, 0, wb.PixelWidth, wb.PixelHeight));
        wb.Unlock();
    }
}