using System.Windows;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI;

public partial class LightEditWindow : Window
{
    public LightEditWindow()
    {
        InitializeComponent();
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LightEditViewModel vm)
        {
            vm.ApplyChanges();
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