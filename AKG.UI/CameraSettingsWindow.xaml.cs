using System.Windows;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI;

public partial class CameraSettingsWindow
{
    public CameraSettingsWindow()
    {
        InitializeComponent();
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CameraSettingsViewModel vm)
        {
            vm.CommitChanges();
        }
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}