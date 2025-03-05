using System.Windows;
using System.Windows.Input;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI;

public partial class LightsSettingsWindow : Window
{
    public LightsSettingsWindow()
    {
        InitializeComponent();
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is LightsListViewModel viewModel && viewModel.SelectedLight != null)
        {
            var editWindow = new LightEditWindow
            {
                DataContext = new LightEditViewModel(viewModel.SelectedLight)
            };

            if (editWindow.ShowDialog() == true)
                // Обновляем данные, если пользователь нажал "OK"
                viewModel.RefreshLights();
        }
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}