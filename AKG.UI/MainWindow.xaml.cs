using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.Core.Renderer;
using Microsoft.WindowsAPICodePack.Dialogs;
using Vector = System.Windows.Vector;

namespace AKG.UI;

public partial class MainWindow
{
    private Scene Scene { get; set; } = new();
    private WriteableBitmap? Wb { get; set; }

    private float RotateSensitivity => MathF.PI / 360.0f;

    private bool _isRotating;
    private Point _lastMousePos;
    
    private Color ForegroundSelectedColor { get; set; } = Colors.Red;
    private Color BackgroundSelectedColor { get; set; } = Colors.White;
    
    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;

        Scene.Camera = new Camera();
        
        Scene.SelectedModelChanged += (s, e) =>
        {
            RedrawScene();
            UpdateModelInfo();
        };
    }
    
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Scene.CanvasHeight = (int) ImagePanel.ActualHeight;
        Scene.CanvasWidth = (int) ImagePanel.ActualWidth;
    }

    private void LoadFile_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                var loadedModel = ObjParser.Parse(dlg.FileName!);
                Wb = new WriteableBitmap(Scene.CanvasWidth, Scene.CanvasHeight, 96, 96, PixelFormats.Bgra32, null);
                ImgDisplay.Source = Wb;

                Scene.Models.Add(loadedModel);
                Scene.SelectedModel = loadedModel;
                Scene.UpdateSelectedModel();
                
                //Scene.UpdateSelectedModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
            }
        }
    }
    
    private void RedrawScene()
    {
        if (Wb == null) return;
        
        WireframeRenderer.ClearBitmap(Wb, BackgroundSelectedColor);

        foreach (var model in Scene.Models)
        {
            WireframeRenderer.DrawWireframe(model, Wb, ForegroundSelectedColor);
        }
    }
    
    private void FileClear_OnClick(object sender, RoutedEventArgs e)
    {
        if (Wb != null)
        {
            WireframeRenderer.ClearBitmap(Wb, BackgroundSelectedColor);
            Scene.Models.Clear();
            Scene.SelectedModel = null;
        }
    }

    private void ImagePanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Scene.SelectedModel != null)
        {
            if (e.Delta > 0)
            {
                Scene.SelectedModel.Scale += Scene.SelectedModel.Delta;
            }
            else
            {
                Scene.SelectedModel.Scale -= Scene.SelectedModel.Delta;
            }
            
            Scene.UpdateSelectedModel();
        }
    }
    
    private void ImagePanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ImagePanel.Focus();
        _isRotating = true;
        _lastMousePos = e.GetPosition(ImagePanel);
        ImagePanel.CaptureMouse();
    }

    private void ImagePanel_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        ImagePanel.ReleaseMouseCapture();
    }
    
    private void ImagePanel_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isRotating && Scene.SelectedModel != null)
        {
            Point currentPos = e.GetPosition(ImgDisplay);
            Vector delta = currentPos - _lastMousePos;
    
            // Если нажата клавиша Shift — вращаем по оси Z, иначе по X и Y.
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
    
            Scene.UpdateSelectedModel();
        }
    }

    private void ImagePanel_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // При правом клике пытаемся выделить модель
        var clickPoint = e.GetPosition(ImgDisplay);
        var pickedModel = Scene.PickModel(clickPoint);
        Scene.SelectedModel = pickedModel;
    }
    
    private void ImagePanel_KeyDown(object sender, KeyEventArgs e)
    {
        if (Scene.SelectedModel == null) return;

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
    
        Scene.UpdateSelectedModel();
    }
    
    private void ForegroundColor_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ForegroundSelectedColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            RedrawScene(); 
        }
    }

    private void BackgroundColor_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            BackgroundSelectedColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            RedrawScene(); 
        }
    }
    
    private void UpdateModelInfo()
    {
        if (Scene.SelectedModel == null) return;
    
        var model = Scene.SelectedModel;
        double rotXDeg = NormalizeAngle(model.Rotation.X * (180.0 / Math.PI));
        double rotYDeg = NormalizeAngle(model.Rotation.Y * (180.0 / Math.PI));
        double rotZDeg = NormalizeAngle(model.Rotation.Z * (180.0 / Math.PI));
    
        string info = $"Vertices: {model.OriginalVertices.Count}\n" +
                      $"Faces: {model.Faces.Count}\n" +
                      $"Scale: {model.Scale:F10}\n" +
                      $"Delta: {model.Delta:F10}\n" +
                      $"Translation: ({model.Translation.X:F2}, {model.Translation.Y:F2}, {model.Translation.Z:F2})\n" +
                      $"Rotation: (X:{rotXDeg:F0}°, Y:{rotYDeg:F0}°, Z:{rotZDeg:F0}°)\n" +
                      $"Model Size: (X: {model.Max.X - model.Min.X:F2}, Y: {model.Max.Y - model.Min.Y:F2}, Z: {model.Max.Z - model.Min.Z:F2});";
        ModelInfoText.Text = info;
    
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

    private void ToggleModelInfoPopup(object sender, RoutedEventArgs e)
    {
        ModelInfoPopup.IsOpen = !ModelInfoPopup.IsOpen;
    }
}