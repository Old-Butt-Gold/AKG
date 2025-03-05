using System.Numerics;
using System.Windows.Media;

namespace AKG.Core.Objects;

public class Light
{
    public Light()
    {
    }

    // Конструктор с параметром direction
    public Light(Vector3 direction)
    {
        Direction = direction;
    }

    // Конструктор с параметрами direction, color и intensity
    public Light(Vector3 direction, Vector3 color, float intensity)
    {
        Direction = direction;
        Color = color;
        Intensity = intensity;
    }

    /// <summary>
    ///     Направление источника света (не нормализовано).
    /// </summary>
    public Vector3 Direction { get; set; } = new(1, 1, 2);

    public Vector3 Color { get; set; } = Vector3.One; // Аналог Colors.White.ToVector3() / 255f

    public float Intensity { get; set; } = 1.0f;

    public static Vector3 ApplyPhongShading(
        List<Light> lights,
        Vector3 normal,
        Vector3 viewDirection,
        Vector3 fragWorld, Vector3 ambientColor, Vector3 ka,
        Vector3 diffuseColor, Vector3 kd, Vector3 specularColor, Vector3 ks, float shininess)
    {
        var ambient = ambientColor * ka;
        var lighting = ambient;

        foreach (var light in lights)
        {
            var lightDir = Vector3.Normalize(light.Direction - fragWorld);

            // Diffuse
            var diff = MathF.Max(Vector3.Dot(normal, lightDir), 0);
            var diffuse = light.Color * diffuseColor * diff * kd;

            // Specular
            var reflectDir = Vector3.Reflect(-lightDir, normal);
            var spec = MathF.Pow(MathF.Max(Vector3.Dot(viewDirection, reflectDir), 0), shininess);
            var specular = light.Color * specularColor * spec * ks;

            lighting += (diffuse + specular) * light.Intensity;
        }

        lighting = Vector3.Clamp(lighting, Vector3.Zero, new Vector3(255, 255, 255));

        return lighting;
    }

    public static Color ApplyLambert(List<Light> lambertLights, Vector3 normal, Color baseColor)
    {
        var totalIntensity = 0f;
        foreach (var light in lambertLights)
        {
            var lightDir = Vector3.Normalize(light.Direction);

            var intensity = MathF.Max(Vector3.Dot(normal, lightDir), 0);

            totalIntensity += intensity;
        }

        // Ограничиваем суммарную интенсивность значением 1
        totalIntensity = MathF.Min(totalIntensity, 1.0f);

        return System.Windows.Media.Color.FromArgb(
            baseColor.A,
            (byte)(baseColor.R * totalIntensity),
            (byte)(baseColor.G * totalIntensity),
            (byte)(baseColor.B * totalIntensity));
    }

    public Vector3 TransformLightToScreen(Matrix4x4 view, Matrix4x4 projection, Matrix4x4 viewport)
    {
        var transformedPosition = Vector4.Transform(Direction, view * projection * viewport);

        if (transformedPosition.W != 0) transformedPosition /= transformedPosition.W;

        return transformedPosition.AsVector3();
    }
}