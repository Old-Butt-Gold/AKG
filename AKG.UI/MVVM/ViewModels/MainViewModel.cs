using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.Core.Renderer;
using AKG.UI.MVVM.Commands;
using AKG.UI.Services.Implementations;
using AKG.UI.Services.Interfaces;
using Microsoft.WindowsAPICodePack.Dialogs;
using Vector = System.Windows.Vector;

namespace AKG.UI.MVVM.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public Scene Scene { get; set; } = new ();

    private WriteableBitmap? _writeableBitmap;

    public WriteableBitmap? WriteableBitmap
    {
        get => _writeableBitmap;
        set
        {
            _writeableBitmap = value;
            OnPropertyChanged(nameof(WriteableBitmap));
        }
    }

    private Color _foregroundColor = Colors.Red;

    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            _foregroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(ForegroundColor));
        }
    }

    private Color _backgroundColor = Colors.White;

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            UpdateView();
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
    
    private string _selectedModelInfo = string.Empty;
    public string SelectedModelInfo
    {
        get => _selectedModelInfo;
        set
        {
            _selectedModelInfo = value;
            OnPropertyChanged(nameof(SelectedModelInfo));
        }
    }

    // Пример команд для загрузки, очистки и редактирования камеры
    public ICommand LoadFileCommand { get; }
    public ICommand ClearSceneCommand { get; }
    public ICommand EditCameraCommand { get; }
    
    // Команды для событий мыши и клавиатуры
    
    public ICommand MouseWheelCommand { get; }
    public ICommand MouseMoveCommand { get; }
    public ICommand MouseLeftButtonDownCommand { get; }
    //public ICommand MouseLeftButtonUpCommand { get; }
    public ICommand MouseRightButtonDownCommand { get; }
    public ICommand KeyDownCommand { get; }
    
    // Комманды для цветовой палитры
    public ICommand PickForegroundColorCommand { get; }
    public ICommand PickBackgroundColorCommand { get; }
    
    public ICommand ToggleModelInfoCommand { get; }

    // Поля для отслеживания состояния вращения
    private Point _lastMousePos;
    private float RotateSensitivity => MathF.PI / 360.0f;

    private IColorPickerService ColorPickerService { get; init; }

    private bool _isModelInfoVisible;

    public bool IsModelInfoVisible
    {
        get => _isModelInfoVisible;
        set
        {
            _isModelInfoVisible = value;
            OnPropertyChanged(nameof(IsModelInfoVisible));
        }
    }

    public MainViewModel()
    {
        ColorPickerService = new ColorPickerService();
        Scene.Camera = new Camera();

        Scene.CanvasWidth = 800;
        Scene.CanvasHeight = 600;

        LoadFileCommand = new RelayCommand(_ => LoadFile());
        ClearSceneCommand = new RelayCommand(_ => ClearScene());
        EditCameraCommand = new RelayCommand(_ => EditCamera());
        
        MouseWheelCommand = new RelayCommand(OnMouseWheel);
        MouseMoveCommand = new RelayCommand(OnMouseMove);
        MouseLeftButtonDownCommand = new RelayCommand(OnMouseLeftButtonDown);
        MouseRightButtonDownCommand = new RelayCommand(OnMouseRightButtonDown);
        KeyDownCommand = new RelayCommand(OnKeyDown);

        PickForegroundColorCommand = new RelayCommand(_ =>
        {
            var color = ColorPickerService.PickColor();
            if (color != null)
            {
                ForegroundColor = color.Value;
            }
        });
        
        PickBackgroundColorCommand = new RelayCommand(_ =>
        {
            var color = ColorPickerService.PickColor();
            if (color != null)
            {
                BackgroundColor = color.Value;
            }
        });

        ToggleModelInfoCommand = new RelayCommand(_ =>
        {
            IsModelInfoVisible = !IsModelInfoVisible;
        });
    }

    private void LoadFile()
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjParser.Parse(dlg.FileName!);
                WriteableBitmap ??= new WriteableBitmap(
                    Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);

                // Добавляем модель в сцену и делаем её выбранной
                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
            }
        }
    }

    private void ClearScene()
    {
        Scene.Models.Clear();
        Scene.SelectedModel = null;
        Scene.Camera = new();
        UpdateView();
        OnPropertyChanged(nameof(Scene));
    }

    private void EditCamera()
    {
        // Создаём окно для редактирования параметров камеры
        var cameraWindow = new CameraSettingsWindow
        {
            DataContext = new CameraSettingsViewModel(Scene.Camera)
        };
        // Показываем окно как модальное
        if (cameraWindow.ShowDialog() == true)
        {
            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }
    
    private void OnMouseWheel(object? parameter)
    {
        if (parameter is MouseWheelEventArgs e)
        {
            if (Scene.SelectedModel != null && 
                (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                if (e.Delta > 0)
                    Scene.SelectedModel.Scale += Scene.SelectedModel.Delta;
                else
                    Scene.SelectedModel.Scale -= Scene.SelectedModel.Delta;
            }
            else
            {
                Scene.Camera.Radius -= e.Delta / 1000.0f;
                if (Scene.Camera.Radius < Scene.Camera.ZNear)
                    Scene.Camera.Radius = Scene.Camera.ZNear;
                if (Scene.Camera.Radius > Scene.Camera.ZFar)
                    Scene.Camera.Radius = Scene.Camera.ZFar;
            }
            
            e.Handled = true;

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }
    
    private void OnMouseMove(object? parameter)
    {
        if (parameter is MouseEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                // Вращение модели
                if (e.LeftButton == MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
                {
                    Point currentPos = e.GetPosition(null);
                    Vector delta = currentPos - _lastMousePos;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X,
                            Scene.SelectedModel.Rotation.Y,
                            Scene.SelectedModel.Rotation.Z - (float)delta.X * RotateSensitivity);
                    }
                    else
                    {
                        Scene.SelectedModel.Rotation = new Vector3(
                            Scene.SelectedModel.Rotation.X + (float)delta.Y * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Y + (float)delta.X * RotateSensitivity,
                            Scene.SelectedModel.Rotation.Z);
                    }

                    _lastMousePos = currentPos;
                
                    UpdateView();
                    OnPropertyChanged(nameof(Scene));
                }
            }
            
            // Вращение камеры
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(null);
                
                float xOffset = (float)(currentPos.X - _lastMousePos.X);
                float yOffset = (float)(currentPos.Y - _lastMousePos.Y);


                Scene.Camera.Zeta -= yOffset * 0.005f;
                Scene.Camera.Phi += xOffset * 0.005f;
                
                if (Scene.Camera.Zeta > Math.PI)
                    Scene.Camera.Zeta = (float)Math.PI - 0.01f;
                if (Scene.Camera.Zeta < 0)
                    Scene.Camera.Zeta = 0.01f;
                
                _lastMousePos = currentPos;
                UpdateView();
                OnPropertyChanged(nameof(Scene));
            }
        }
    }

    private void OnMouseLeftButtonDown(object? parameter)
    {
        // Для начала вращения – сохраняем позицию мыши и выставляем флаг
        if (parameter is MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(null);
            if (e.OriginalSource is UIElement uiElement)
            {
                uiElement.Focus();
            }
        }
    }
    
    private void OnMouseRightButtonDown(object? parameter)
    {
        if (parameter is MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(null);
            Point clickPoint = _lastMousePos;
            var pickedModel = Scene.PickModel(clickPoint);
            Scene.SelectedModel = pickedModel;
            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    private void OnKeyDown(object? parameter)
    {
        if (parameter is KeyEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                if (e.Key == Key.Delete)
                {
                    Scene.Models.Remove(Scene.SelectedModel);
                    Scene.SelectedModel = Scene.Models.FirstOrDefault();

                    UpdateView();
                    OnPropertyChanged(nameof(Scene));
                    return;
                }

                var step = Scene.SelectedModel.GetOptimalTranslationStep();

                switch (e.Key)
                {
                    case Key.Right:
                        Scene.SelectedModel.Translation += new Vector3(step.X, 0, 0);
                        break;
                    case Key.Left:
                        Scene.SelectedModel.Translation += new Vector3(-step.X, 0, 0);
                        break;
                    case Key.Up:
                        Scene.SelectedModel.Translation += new Vector3(0, step.Y, 0);
                        break;
                    case Key.Down:
                        Scene.SelectedModel.Translation += new Vector3(0, -step.Y, 0);
                        break;
                    case Key.S:
                        Scene.SelectedModel.Translation += new Vector3(0, 0, -step.Z);
                        break;
                    case Key.W:
                        Scene.SelectedModel.Translation += new Vector3(0, 0, step.Z);
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Left:
                        Scene.Camera.Target += new Vector3(-0.5f, 0, 0);
                        break;
                    case Key.Right:
                        Scene.Camera.Target += new Vector3(0.5f, 0, 0);
                        break;
                    case Key.Up:
                        Scene.Camera.Target += new Vector3(0.0f, 0.5f, 0);
                        break;
                    case Key.Down:
                        Scene.Camera.Target += new Vector3(0.0f, -0.5f, 0);
                        break;
                }
            }

            UpdateView();
            OnPropertyChanged(nameof(Scene));
        }
    }

    /// <summary>
    /// Вызывает методы перерисовки: очищает холст, отрисовывает объекты и выделение.
    /// </summary>
    public void UpdateView()
    {
        if (WriteableBitmap == null) return;

        Scene.Camera.ChangeEye();
        
        /*if (Scene.SelectedModel != null)
        {
            Scene.UpdateSelectedModel();
        }*/
        
        Scene.UpdateAllModels();
        
        WireframeRenderer.ClearBitmap(WriteableBitmap, BackgroundColor);
        
        foreach (var model in Scene.Models)
        {
            //TriangleRasterizer.FillTriangles(model, WriteableBitmap, ForegroundColor);
            WireframeRenderer.DrawWireframe(model, WriteableBitmap, ForegroundColor, Scene.Camera);
        }
        
        if (Scene.SelectedModel is not null && WriteableBitmap is not null)
            WireframeRenderer.Draw3DSelectionHighlight(Scene, Scene.SelectedModel, WriteableBitmap, Colors.Aqua);

        UpdateSelectedModelInfo();
        
        // Сообщаем, что обновился WriteableBitmap
        OnPropertyChanged(nameof(WriteableBitmap));
    }
    
    /// <summary>
    /// Обновляет строку с информацией о выбранной модели.
    /// </summary>
    private void UpdateSelectedModelInfo()
    {
        if (Scene.SelectedModel == null)
        {
            SelectedModelInfo = "No model selected.";
            return;
        }
        var model = Scene.SelectedModel;
        double rotXDeg = NormalizeAngle(model.Rotation.X * (180.0 / Math.PI));
        double rotYDeg = NormalizeAngle(model.Rotation.Y * (180.0 / Math.PI));
        double rotZDeg = NormalizeAngle(model.Rotation.Z * (180.0 / Math.PI));

        SelectedModelInfo =
            $"Vertices: {model.OriginalVertices.Count}\n" +
            $"Faces: {model.Faces.Count}\n" +
            $"Scale: {model.Scale:F10}\n" +
            $"Delta: {model.Delta:F10}\n" +
            $"Translation: ({model.Translation.X:F2}, {model.Translation.Y:F2}, {model.Translation.Z:F2})\n" +
            $"Rotation: (X:{rotXDeg:F0}°, Y:{rotYDeg:F0}°, Z:{rotZDeg:F0}°)\n" +
            $"Model Size: (X: {model.Max.X - model.Min.X:F2}, Y: {model.Max.Y - model.Min.Y:F2}, Z: {model.Max.Z - model.Min.Z:F2});";

        double NormalizeAngle(double angle)
        {
            angle %= 360;
            if (angle > 180)
                angle -= 360;
            else if (angle <= -180)
                angle += 360;
            return angle;
        }
    }

    // Реализация INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}