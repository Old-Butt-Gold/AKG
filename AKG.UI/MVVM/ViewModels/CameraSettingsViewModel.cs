using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;
using AKG.Core.Objects;
using AKG.UI.MVVM.Commands;

namespace AKG.UI.MVVM.ViewModels;

public class CameraSettingsViewModel : INotifyPropertyChanged
{
    private readonly Camera _camera;

    // Для отмены действий
    private Vector3 _eye;
    private Vector3 _target;
    private Vector3 _up;
    private float _fov;
    private float _aspect;
    private float _zNear;
    private float _zFar;

    public CameraSettingsViewModel(Camera camera)
    {
        _camera = camera;
        _eye = camera.Eye;
        _target = camera.Target;
        _up = camera.Up;
        _fov = camera.Fov;
        _aspect = camera.Aspect;
        _zNear = camera.ZNear;
        _zFar = camera.ZFar;
    }

    public Vector3 Eye
    {
        get => _eye;
        set { _eye = value; OnPropertyChanged(nameof(Eye)); }
    }
    public Vector3 Target
    {
        get => _target;
        set { _target = value; OnPropertyChanged(nameof(Target)); }
    }
    public Vector3 Up
    {
        get => _up;
        set { _up = value; OnPropertyChanged(nameof(Up)); }
    }
    public float Fov
    {
        get => _fov;
        set { _fov = value; OnPropertyChanged(nameof(Fov)); }
    }
    public float Aspect
    {
        get => _aspect;
        set { _aspect = value; OnPropertyChanged(nameof(Aspect)); }
    }
    public float ZNear
    {
        get => _zNear;
        set { _zNear = value; OnPropertyChanged(nameof(ZNear)); }
    }
    public float ZFar
    {
        get => _zFar;
        set { _zFar = value; OnPropertyChanged(nameof(ZFar)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public void CommitChanges()
    {
        _camera.Eye = Eye;
        _camera.Target = Target;
        _camera.Up = Up;
        _camera.Fov = Fov;
        _camera.Aspect = Aspect;
        _camera.ZNear = ZNear;
        _camera.ZFar = ZFar;
    }
    
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}