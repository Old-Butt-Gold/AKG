using System.IO;
using System.Windows.Media.Imaging;

namespace AKG.Core.Objects;

public static class TextureLoader
{
    private static readonly Dictionary<string, BitmapImage> _cache = new();

    public static BitmapImage? Load(string path)
    {
        if (_cache.TryGetValue(path, out var image))
            return image;

        if (!File.Exists(path)) return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        _cache[path] = bitmap;
        return bitmap;
    }
}