using System.ComponentModel;
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
        EditLightCommand = new RelayCommand(_ => EditSelectedLight());
    }

    public List<Light> Lights { get; }

    public Light? SelectedLight
    {
        get => _selectedLight;
        set
        {
            _selectedLight = value;
            OnPropertyChanged(nameof(SelectedLight));
        }
    }

    public ICommand EditLightCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void EditSelectedLight()
    {
        if (SelectedLight == null) return;

        var editWindow = new LightEditWindow
        {
            DataContext = new LightEditViewModel(SelectedLight)
        };

        if (editWindow.ShowDialog() == true)
            // Обновляем данные, если пользователь нажал "OK"
            OnPropertyChanged(nameof(Lights));
    }

    public void RefreshLights()
    {
        OnPropertyChanged(nameof(Lights));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}