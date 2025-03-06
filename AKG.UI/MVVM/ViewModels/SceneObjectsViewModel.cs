using System.Collections.ObjectModel;
using System.Windows.Input;
using AKG.Core.Objects;
using AKG.UI.MVVM.Commands;

namespace AKG.UI.MVVM.ViewModels
{
    public class SceneObjectsViewModel
    {
        public ObservableCollection<ObjModel> Objects { get; set; }

        public ICommand SelectObjectCommand { get; }

        public SceneObjectsViewModel(Scene scene)
        {
            Objects = new ObservableCollection<ObjModel>(scene.Models);
            SelectObjectCommand = new RelayCommand(SelectObject);
        }

        private void SelectObject(object? parameter)
        {
            if (parameter is ObjModel selectedObject)
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