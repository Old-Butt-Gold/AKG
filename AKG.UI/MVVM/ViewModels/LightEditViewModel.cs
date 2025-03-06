using System.ComponentModel;
using System.Numerics;
using AKG.Core.Objects;

namespace AKG.UI.MVVM.ViewModels;

public class LightEditViewModel : INotifyPropertyChanged
{
    private Light _selectedLight;
    private Vector3 _tempColor;
    private Vector3 _tempDirection;
    private float _tempIntensity;

    public LightEditViewModel(Light selectedLight)
    {
        _selectedLight = selectedLight;
        _tempDirection = selectedLight.Direction;
        _tempColor = selectedLight.Color;
        _tempIntensity = selectedLight.Intensity;
    }

    public Vector3 TempDirection
    {
        get => _tempDirection;
        set
        {
            _tempDirection = value;
            OnPropertyChanged(nameof(TempDirection));
        }
    }

    public Vector3 TempColor
    {
        get => _tempColor;
        set
        {
            _tempColor = value;
            OnPropertyChanged(nameof(TempColor));
        }
    }

    public float TempIntensity
    {
        get => _tempIntensity;
        set
        {
            _tempIntensity = value;
            OnPropertyChanged(nameof(TempIntensity));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyChanges()
    {
        _selectedLight.Direction = _tempDirection;
        _selectedLight.Color = _tempColor;
        _selectedLight.Intensity = _tempIntensity;
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}