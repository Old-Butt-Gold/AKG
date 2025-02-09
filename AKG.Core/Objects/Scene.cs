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
    
    // Добавляем свойство для выбранной модели с событием
    private ObjModel? _selectedModel;
    public ObjModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (_selectedModel != value)
            {
                _selectedModel = value;
                SelectedModelChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    
    // Событие, вызываемое при изменении выбранной модели.
    public event EventHandler? SelectedModelChanged;
    
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
        
        Redraw();
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
        
        Redraw();
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
        // Перебираем модели, можно выбрать ту, чья проецированная область содержит clickPoint
        foreach (var model in Models)
        {
            Rect bb = GetScreenBoundingBox(model);
            if (bb.Contains(clickPoint))
                return model;
        }
        return null;
    }
    
    /// <summary>
    /// Вычисляет bounding box для модели на основе её пересчитанных экранных координат (TransformedVertices).
    /// </summary>
    private Rect GetScreenBoundingBox(ObjModel model)
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

    public void Redraw()
    {
        SelectedModelChanged?.Invoke(this, EventArgs.Empty);
    }
}