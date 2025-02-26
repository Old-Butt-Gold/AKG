using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AKG.Core.Objects
{
    public static class TextureSampler
    {
        // Кэш для хранения массивов пикселей по каждой текстуре.
        private static readonly Dictionary<BitmapImage, byte[]> _textureCache = [];

        /// <summary>
        /// Выбирает (sample) цвет из текстуры по заданным координатам u и v.
        /// Предполагается, что u,v ∈ [0,1]. Координата v инвертируется, поскольку
        /// изображения WPF имеют начало координат в верхнем левом углу.
        /// </summary>
        public static Color Sample(BitmapImage texture, float u, float v)
        {
            int width = texture.PixelWidth;
            int height = texture.PixelHeight;

            // Приводим u,v к пиксельным координатам.
            int x = (int)(u * width);
            int y = (int)((1 - v) * height);

            x = Math.Clamp(x, 0, width - 1);
            y = Math.Clamp(y, 0, height - 1);

            if (!_textureCache.TryGetValue(texture, out var pixels))
            {
                int stride = width * 4; // предполагается формат BGRA (4 байта на пиксель)
                pixels ??= new byte[height * stride];
                texture.CopyPixels(pixels, stride, 0);
                _textureCache[texture] = pixels;
            }

            int index = (y * width + x) * 4;
            // Читаем пиксель в формате BGRA
            byte b = pixels[index];
            byte g = pixels[index + 1];
            byte r = pixels[index + 2];
            byte a = pixels[index + 3];

            return Color.FromArgb(a, r, g, b);
        }
    }
}