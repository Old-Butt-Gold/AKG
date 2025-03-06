using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using AKG.Core.Objects;

namespace AKG.UI.MVVM.ViewModels;

public class LightViewModel : BaseViewModel
{

    public LightViewModel(Light light)
    {
        _direction = light.Direction;
        _color = light.Color;
        _intensity = light.Intensity;
    }

    public Light ToLight()
    {
        return new Light(_direction, _color, _intensity);
    }

    private Vector3 _direction;
    private Vector3 _color;
    private float _intensity;

    public Vector3 Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            OnPropertyChanged();
        }
    }

    public Vector3 Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged();
        }
    }

    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = value;
            OnPropertyChanged();
        }
    }
}