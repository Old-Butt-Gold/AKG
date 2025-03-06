using System.Windows;
using System.Windows.Controls;
using AKG.Core.Parser;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI
{
    public partial class MaterialsWindow : Window
    {
        public MaterialsWindow()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Получаем выбранный материал
            var listBox = sender as ListBox;
            var selectedMaterial = listBox?.SelectedItem as Material;

            if (selectedMaterial != null)
            {
                // Открываем окно с параметрами выбранного материала
                var materialSettingsWindow = new MaterialSettingsWindow
                {
                    DataContext = new MaterialSettingsViewModel(selectedMaterial)
                };
                materialSettingsWindow.ShowDialog();
            }
        }
    }
}