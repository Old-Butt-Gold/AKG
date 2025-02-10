using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.Core.Renderer;
using AKG.UI.MVVM.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;
using Vector = System.Windows.Vector;

namespace AKG.UI;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;
    }
    
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Scene.CanvasHeight = (int)ImagePanel.ActualHeight;
            vm.Scene.CanvasWidth = (int)ImagePanel.ActualWidth;
        }
    }
    
    private void ForegroundColor_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ForegroundColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            }
        }
    }

    private void BackgroundColor_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.BackgroundColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            }
        }
    }

    private void ToggleModelInfoPopup(object sender, RoutedEventArgs e)
    {
        ModelInfoPopup.IsOpen = !ModelInfoPopup.IsOpen;
    }
}