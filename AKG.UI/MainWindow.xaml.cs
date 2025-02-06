using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Parser;
using AKG.Core.Renderer;
using Microsoft.WindowsAPICodePack.Dialogs;
using Vector = System.Windows.Vector;

namespace AKG.UI;

public partial class MainWindow
{
    private ObjModel? ObjModel { get; set; }
    private WriteableBitmap? Wb { get; set; }

    private float FloatAmount { get; init; } = 2.5f;

    private float RotateSensitivity { get; init; } = MathF.PI / 360.0f;
    
    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;
    }

    private void LoadFile_OnClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new CommonOpenFileDialog();
        dlg.Filters.Add(new CommonFileDialogFilter("OBJ Files", "*.obj"));
        if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
        {
            try
            {
                ObjModel = ObjParser.Parse(dlg.FileName!);

                int width = (int)(ImagePanel.ActualWidth > 0 ? ImagePanel.ActualWidth : 800);
                int height = (int)(ImagePanel.ActualHeight > 0 ? ImagePanel.ActualHeight : 600);
                ObjModel.WindowSize = new(width, height);

                Wb = new WriteableBitmap(ObjModel.WindowSize.Width, ObjModel.WindowSize.Height, 96, 96, PixelFormats.Bgra32, null);
                ImgDisplay.Source = Wb;
                
                RedrawModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
            }
        }
    }
    private void RedrawModel()
    {
        if (Wb == null || ObjModel == null) return;
        WireframeRenderer.ClearBitmap(Wb, Colors.White);
        ObjModel.UpdateImage();
        WireframeRenderer.DrawWireframe(ObjModel, Wb, Colors.Red);
    }
    
    private void FileClear_OnClick(object sender, RoutedEventArgs e)
    {
        if (Wb != null)
        {
            WireframeRenderer.ClearBitmap(Wb, Colors.White);
            ObjModel = null;
        }
    }

    private void ImagePanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ObjModel != null)
        {
            if (e.Delta > 0)
            {
                ObjModel.Scale += ObjModel.Delta;
            }
            else
            {
                ObjModel.Scale -= ObjModel.Delta;
            }

            ObjModel.Delta = ObjModel.Scale / 10.0f;
            RedrawModel();
        }
    }

    private bool _isRotating;
    private Point _lastMousePos;
    
    private void ImagePanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isRotating = true;
        _lastMousePos = e.GetPosition(ImgDisplay);
        ImgDisplay.CaptureMouse();
    }

    private void ImagePanel_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        ImgDisplay.ReleaseMouseCapture();
    }
    
    private void ImagePanel_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isRotating && ObjModel != null)
        {
            Point currentPos = e.GetPosition(ImgDisplay);
            Vector delta = currentPos - _lastMousePos;

            Matrix4x4 matrix;
            
            // Если нажата клавиша Shift, вращаем по оси Z, иначе по X и Y.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                matrix = Matrix4x4.CreateRotationZ((float)-delta.X * RotateSensitivity);
            }
            else
            {
                matrix = Matrix4x4.CreateRotationX((float)delta.Y * RotateSensitivity)
                         * Matrix4x4.CreateRotationY((float)delta.X * RotateSensitivity);
            }
            _lastMousePos = currentPos;
            
            ObjModel.ApplyTransformation(matrix);
            RedrawModel();
        }
    }

    private void ImagePanel_KeyDown(object sender, KeyEventArgs e)
    {
        if (ObjModel is null) return;
        
        switch (e.Key)
        {
            case Key.Right:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(FloatAmount, 0, 0));
                break;
            case Key.Left:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(-FloatAmount, 0, 0));
                break;
            case Key.Up:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(0, FloatAmount, 0));
                break;
            case Key.Down:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(0, -FloatAmount, 0));
                break;
            case Key.S:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(0, 0, -FloatAmount));
                break;
            case Key.W:
                ObjModel.ApplyTransformation(Matrix4x4.CreateTranslation(0, 0, FloatAmount));
                break;
        }
        RedrawModel();
    }
}