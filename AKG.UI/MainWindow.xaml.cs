using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Scene.CanvasHeight = (int)ImagePanel.ActualHeight;
            vm.Scene.CanvasWidth = (int)ImagePanel.ActualWidth;
        }
    }

    private void ImgDisplay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("MouseDoubleClick event triggered directly");
    }
    private void Image_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.MouseDoubleClickCommand.CanExecute(null))
        {
            viewModel.MouseDoubleClickCommand.Execute(null);
        }
        e.Handled = true;
    }
}