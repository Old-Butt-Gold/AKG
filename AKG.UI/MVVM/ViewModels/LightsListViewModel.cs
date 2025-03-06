using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;
using AKG.Core.Objects;
using AKG.UI.MVVM.Commands;

namespace AKG.UI.MVVM.ViewModels;

public class LightsListViewModel : INotifyPropertyChanged
{
    private Light? _selectedLight;

    public LightsListViewModel(List<Light> lights)
    {
        Lights = lights;
        EditLightCommand = new RelayCommand(_ => EditSelectedLight(), _ => SelectedLight != null);
        AddLightCommand = new RelayCommand(_ => AddNewLight());
        RemoveLightCommand = new RelayCommand(_ => RemoveSelectedLight(), _ => SelectedLight != null);
    }

    public List<Light> Lights { get; }

    public Light? SelectedLight
    {
        get => _selectedLight;
        set
        {
            _selectedLight = value;
            OnPropertyChanged(nameof(SelectedLight));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ICommand EditLightCommand { get; }
    public ICommand AddLightCommand { get; }
    public ICommand RemoveLightCommand { get; }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddNewLight()
    {
        var newLight = new Light(
            new Vector3(0, 0, 0),  // default position
            new Vector3(1, 1, 1),   // default color (white)
            1.0f                    // default intensity
        );

        var editWindow = new LightEditWindow
        {
            DataContext = new LightEditViewModel(newLight)
        };

        if (editWindow.ShowDialog() == true)
        {
            Lights.Add(newLight);
            SelectedLight = newLight;
            OnPropertyChanged(nameof(Lights));
        }
    }

    private void RemoveSelectedLight()
    {
        if (SelectedLight != null)
        {
            Lights.Remove(SelectedLight);
            SelectedLight = null;
            OnPropertyChanged(nameof(Lights));
        }
    }

    private void EditSelectedLight()
    {
        if (SelectedLight == null) return;

        var editWindow = new LightEditWindow
        {
            DataContext = new LightEditViewModel(SelectedLight)
        };

        if (editWindow.ShowDialog() == true)
        {
            OnPropertyChanged(nameof(Lights));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public void RefreshLights()
    {
        OnPropertyChanged(nameof(Lights));
    }
}