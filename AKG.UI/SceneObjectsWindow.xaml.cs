using System.Windows;
using System.Windows.Controls;
using AKG.Core.Objects;
using AKG.UI.MVVM.ViewModels;

namespace AKG.UI
{
    public partial class SceneObjectsWindow : Window
    {
        public SceneObjectsWindow()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Получаем выбранный объект
            var listBox = sender as ListBox;
            var selectedObject = listBox?.SelectedItem as ObjModel;

            if (selectedObject != null)
            {
                // Открываем окно с материалами выбранного объекта
                var materialsWindow = new MaterialsWindow
                {
                    DataContext = new MaterialsViewModel(selectedObject)
                };
                materialsWindow.ShowDialog();
            }
        }
    }
}