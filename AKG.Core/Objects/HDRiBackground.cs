using System.IO;
using System.Numerics;
using StbImageSharp;

namespace AKG.Core.Objects;

public class HDRiBackground
{
    private Vector3[] _pixels = [];
    private int _width;
    private int _height;

    public void LoadFromHdrFile(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);
        
        _width = image.Width;
        _height = image.Height;
        _pixels = new Vector3[_width * _height];

        // Используем данные в формате float для HDR
        var floatData = image.Data;
        
        Parallel.For(0, _pixels.Length, i =>
        {
            int offset = i * 3;
            _pixels[i] = new Vector3(
                floatData[offset],
                floatData[offset + 1],
                floatData[offset + 2]
            );
        });
    }

    public Vector3 SampleSpherical(Vector3 direction)
    {
        float u = (MathF.Atan2(direction.Z, direction.X) + MathF.PI) / (2 * MathF.PI);
        float v = MathF.Acos(direction.Y) / MathF.PI;
        
        return SampleUV(u, v);
    }

    public Vector3 SampleUV(float u, float v)
    {
        int x = (int)(u * _width) % _width;
        int y = (int)(v * _height) % _height;
        return _pixels[y * _width + x];
    }
}