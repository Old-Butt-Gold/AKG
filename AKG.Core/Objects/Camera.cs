using System.Numerics;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Objects;

public class Camera
{
    public Vector3 Eye { get; set; } = new Vector3(1.0f, 1.0f, MathF.PI);
    public Vector3 Target { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float Fov { get; set; } = MathF.PI / 4.0f;
    public float Aspect { get; set; } = 16f / 9f;
    public float ZNear { get; set; } = 1f;
    public float ZFar { get; set; } = 100f;

    public Matrix4x4 GetViewMatrix() =>
        Transformations.CreateViewMatrix(Eye, Target, Up);

    public Matrix4x4 GetProjectionMatrix() =>
        Transformations.CreatePerspectiveProjection(Fov, Aspect, ZNear, ZFar);
}