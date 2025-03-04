using System.Numerics;
using System.Windows;
using AKG.Core.Parser;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Objects;

public class Scene
{
    // Список моделей, отображаемых на холсте.
    public List<ObjModel> Models { get; } = [];

    // Список лучей/точек света, падающих на 3D объект
    public List<Light> Lights { get; } = [];

    // Камера для сцены.
    public Camera Camera { get; set; } = new();
        
    // Размеры холста (например, размер WriteableBitmap).
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }

    public Matrix4x4 GetViewportMatrix() =>
        Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight);
    
    public ObjModel? SelectedModel { get; set; }
    
    public void UpdateAllModels()
    {
        var view = Camera.GetViewMatrix();
        var projection = Camera.GetProjectionMatrix();
        var viewport = GetViewportMatrix();
    
        foreach (var model in Models)
        {
            UpdateModelTransform(model, view, projection, viewport);
        }
        
    }
    
    public void UpdateSelectedModel()
    {
        if (SelectedModel is null)
            return;

        var view = Camera.GetViewMatrix();
        var projection = Camera.GetProjectionMatrix();
        var viewport = GetViewportMatrix();

        UpdateModelTransform(SelectedModel, view, projection, viewport);
        
    }
    
    private void UpdateModelTransform(ObjModel model, Matrix4x4 view, Matrix4x4 projection, Matrix4x4 viewport)
    {
        // Вычисляем мировую матрицу для модели на основе её локальных параметров:
        var world = Transformations.CreateWorldTransform(
            model.Scale,
            Matrix4x4.CreateFromYawPitchRoll(model.Rotation.Y, model.Rotation.X, model.Rotation.Z),
            model.Translation);

        // Композиция матриц: World * View * Projection * Viewport
        var finalTransform = world * view * projection * viewport;
        model.ApplyFinalTransformation(finalTransform, Camera);
    }
    
    public ObjModel? PickModel(Point clickPoint)
    {
        ObjModel? pickedModel = null;
        float bestDepth = float.MaxValue;
 
        foreach (var model in Models)
        {
            if (model.TransformedVertices.Length == 0)
                continue;
 
            // Вычисляем экранный bounding box для модели
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            float modelDepth = float.MaxValue;
 
            foreach (var v in model.TransformedVertices)
            {
                minX = MathF.Min(minX, v.X);
                minY = MathF.Min(minY, v.Y);
                maxX = MathF.Max(maxX, v.X);
                maxY = MathF.Max(maxY, v.Y);
                // Используем минимальное Z (ближайшую к камере точку)
                modelDepth = MathF.Min(modelDepth, v.Z);
            }
 
            // Проверяем, находится ли точка клика внутри bounding box
            if (clickPoint.X >= minX && clickPoint.X <= maxX &&
                clickPoint.Y >= minY && clickPoint.Y <= maxY)
            {
                // Если моделей несколько, выбираем ту, которая ближе к камере (меньший Z)
                if (modelDepth < bestDepth)
                {
                    bestDepth = modelDepth;
                    pickedModel = model;
                }
            }
        }
 
        return pickedModel;
    }
}