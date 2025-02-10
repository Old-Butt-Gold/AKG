using System.Numerics;
using System.Windows;
using AKG.Core.Parser;
using AKG.Core.VectorTransformations;

namespace AKG.Core.Objects;

public class Scene
{
    // Список моделей, отображаемых на холсте.
    public List<ObjModel> Models { get; } = new();
        
    // Камера для сцены.
    public Camera Camera { get; set; } = new Camera();
        
    // Размеры холста (например, размер WriteableBitmap).
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }

    public Matrix4x4 GetViewportMatrix() =>
        Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight);
    
    public ObjModel? SelectedModel { get; set; }
    
    /// <summary>
    /// Для каждой модели рассчитывает итоговую матрицу преобразования:
    /// World (локальные параметры модели) * View (из камеры) * Projection (из камеры) * Viewport.
    /// Затем обновляет трансформированные вершины модели.
    /// </summary>
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
    
    /// <summary>
    /// Обновляет трансформации только для выбранной модели.
    /// </summary>
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
    
    /// <summary>
    /// Пытается выбрать модель, экранный bounding box которой содержит точку clickPoint.
    /// </summary>
    /// <param name="clickPoint">Координаты клика (в системе координат компонента Image)</param>
    /// <returns>Найденная модель или null</returns>
    public ObjModel? PickModel(Point clickPoint)
    {
        List<ObjModel> candidates = [];

        // Собираем все модели, чей bounding box содержит clickPoint.
        foreach (var model in Models)
        {
            Rect bb = GetScreenBoundingBox(model);
            if (bb.Contains(clickPoint))
                candidates.Add(model);
        }
    
        if (candidates.Count == 0)
            return null;

        // Для каждого кандидата вычисляем параметр глубины.
        // Например, можно взять среднее значение z-координаты его пересчитанных вершин.
        // (Обратите внимание, что система координат после перспективного преобразования может требовать корректировки критериев.)
        candidates.Sort((a, b) =>
        {
            var depthA = GetModelAverageDepth(a);
            var depthB = GetModelAverageDepth(b);
            return depthA.CompareTo(depthB);
        });
    
        // Если в вашей системе меньшие z (или большее, в зависимости от соглашений) означает, что объект ближе,
        // можно выбрать, например, последний элемент (наиболее удалённый) или предложить пользователю циклически переключаться.
        return candidates[^1];
        
        float GetModelAverageDepth(ObjModel model)
        {
            return model.TransformedVertices.Length == 0
                ? float.MaxValue
                : model.TransformedVertices.Sum(v => v.Z) / model.TransformedVertices.Length;
        }
    }
    
    /// <summary>
    /// Вычисляет bounding box для модели на основе её пересчитанных экранных координат (TransformedVertices).
    /// </summary>
    public Rect GetScreenBoundingBox(ObjModel model)
    {
        if (model.TransformedVertices.Length == 0)
            return Rect.Empty;
    
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
    
        foreach (var v in model.TransformedVertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
        }
    
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}