using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AKG.Core.Parser;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI;

public partial class MaterialListWindow : Window
{
    public MaterialListWindow()
    {
        InitializeComponent();
    }

    private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MaterialListViewModel vm)
        {
            if (sender is ListView listView)
            {
                var selectedItem = listView.SelectedItem as KeyValuePair<string, Material>?;
                var value = selectedItem!.Value;
                
                var window = new MaterialEditorWindow
                {
                    DataContext = new MaterialEditorViewModel(value.Value)
                };
                window.Show();
            }
        }
    }
}