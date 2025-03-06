using System.Collections.ObjectModel;
using System.Windows.Input;
using AKG.Core.Objects;
using AKG.Core.Parser;
using AKG.UI.MVVM.Commands;

namespace AKG.UI.MVVM.ViewModels
{
    public class MaterialsViewModel
    {
        public ObservableCollection<Material> Materials { get; set; }

        public ICommand SelectMaterialCommand { get; }

        public MaterialsViewModel(ObjModel objModel)
        {
            Materials = new ObservableCollection<Material>(objModel.Materials.Values);
            SelectMaterialCommand = new RelayCommand(SelectMaterial);
        }

        private void SelectMaterial(object? parameter)
        {
            if (parameter is Material selectedMaterial)
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